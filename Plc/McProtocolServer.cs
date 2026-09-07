using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MaterialControlSimulator.Plc
{
    public class McProtocolServer
    {
        private readonly PlcMemory _memory;
        private readonly PlcBindingManager _bindingManager;

        private TcpListener? _listener;
        private CancellationTokenSource? _cts;

        public bool IsRunning => _listener != null;

        public McProtocolServer(
            PlcMemory memory,
            PlcBindingManager bindingManager)
        {
            _memory = memory;
            _bindingManager = bindingManager;
        }

        // ============================================================
        // START / STOP
        // ============================================================

        public async Task StartAsync(
            int port = 5000)
        {
            if (IsRunning)
                return;

            _cts = new CancellationTokenSource();

            _listener = new TcpListener(
                IPAddress.Any,
                port);

            _listener.Start();

            Debug.WriteLine(
                $"MC Server Started : {port}");

            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    TcpClient client;

                    try
                    {
                        client =
                            await _listener.AcceptTcpClientAsync(
                                _cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(
                            $"MC Accept ERROR : {ex.Message}");

                        continue;
                    }

                    // Accept loop를 막지 않음
                    _ = HandleClientAsync(
                        client,
                        _cts.Token);
                }
            }
            finally
            {
                try
                {
                    _listener?.Stop();
                }
                catch
                {
                }

                _listener = null;
            }
        }

        public void Stop()
        {
            try
            {
                _cts?.Cancel();
                _listener?.Stop();
            }
            catch
            {
            }
            finally
            {
                _listener = null;

                _cts?.Dispose();
                _cts = null;
            }

            Debug.WriteLine(
                "MC Server Stopped");
        }

        // ============================================================
        // CLIENT
        // ============================================================

        private async Task HandleClientAsync(
            TcpClient client,
            CancellationToken token)
        {
            using (client)
            {
                // 불필요한 지연 방지
                client.NoDelay = true;

                NetworkStream stream =
                    client.GetStream();

                Debug.WriteLine(
                    $"MC Client Connected : " +
                    $"{client.Client.RemoteEndPoint}");

                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        byte[]? request = null;
                        try
                        {
                            request = await ReadFrameAsync(stream, token);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(
                                 $"[MC READ ERROR] {DateTime.Now:HH:mm:ss.fff} " +
                                 $"Remote={client.Client.RemoteEndPoint}");

                            Debug.WriteLine(ex.ToString());
                            throw;
                        }

                        if (request == null)
                            break;

                        byte[] response;

                        try
                        {
                            response = ProcessRequest(request);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(
                                $"MC Process ERROR : {ex}");

                            response =
                                CreateErrorResponse(
                                    request,
                                    0xC051);
                        }

                        if (response.Length == 0)
                            continue;

                        //Debug.WriteLine(
                        //    $"REQ  {DateTime.Now:HH:mm:ss.fff} " +
                        //    $"Len={request.Length} " +
                        //    $"{BitConverter.ToString(request)}");

                        //Debug.WriteLine(
                        //    $"RESP {DateTime.Now:HH:mm:ss.fff} " +
                        //    $"Len={response.Length} " +
                        //    $"{BitConverter.ToString(response)}");


                        await stream.WriteAsync(
                            response.AsMemory(),
                            token);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (IOException)
                {
                }
                catch (SocketException)
                {
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MC PROCESS ERROR] {DateTime.Now:HH:mm:ss.fff}");

                    Debug.WriteLine(ex.ToString());

                    Debug.WriteLine(
                        $"MC Client ERROR : {ex.Message}");
                }
                finally
                {
                    Debug.WriteLine(
                        "MC Client Disconnected");
                }
            }
        }

        // ============================================================
        // FRAME READER
        // ============================================================

        private static async Task<byte[]?>
            ReadFrameAsync(
                NetworkStream stream,
                CancellationToken token)
        {
            // 3E Binary header
            const int headerLength = 9;

            byte[] header =
                new byte[headerLength];

            bool headerOk =
                await ReadExactAsync(
                    stream,
                    header,
                    headerLength,
                    token);

            if (!headerOk)
                return null;

            // Request subheader
            if (header[0] != 0x50 ||
                header[1] != 0x00)
            {
                return null;
            }

            int dataLength =
                header[7] |
                (header[8] << 8);

            if (dataLength <= 0)
                return null;

            // 비정상 packet 방어
            if (dataLength > 65535)
                return null;

            byte[] frame =
                new byte[
                    headerLength +
                    dataLength];

            Buffer.BlockCopy(
                header,
                0,
                frame,
                0,
                headerLength);

            byte[] body =
                new byte[dataLength];

            bool bodyOk =
                await ReadExactAsync(
                    stream,
                    body,
                    dataLength,
                    token);

            if (!bodyOk)
                return null;

            Buffer.BlockCopy(
                body,
                0,
                frame,
                headerLength,
                dataLength);


            return frame;
        }

        private static async Task<bool>
            ReadExactAsync(
                NetworkStream stream,
                byte[] buffer,
                int count,
                CancellationToken token)
        {
            int offset = 0;

            while (offset < count)
            {
                int read =
                    await stream.ReadAsync(
                        buffer.AsMemory(
                            offset,
                            count - offset),
                        token);

                if (read <= 0)
                    return false;

                offset += read;
            }

            return true;
        }

        // ============================================================
        // REQUEST DISPATCH
        // ============================================================

        private byte[] ProcessRequest(
            byte[] request)
        {
            if (request.Length < 15)
                return Array.Empty<byte>();

            ushort command =
                ReadUInt16(
                    request,
                    11);

            ushort subCommand =
                ReadUInt16(
                    request,
                    13);

            return command switch
            {
                // Batch Read
                0x0401 =>
                    ProcessBatchRead(
                        request,
                        subCommand),

                // Random Read
                0x0403 =>
                    ProcessRandomRead(
                        request),

                // Batch Write
                0x1401 =>
                    ProcessBatchWrite(
                        request,
                        subCommand),

                _ =>
                    CreateErrorResponse(
                        request,
                        0xC059)
            };
        }

        // ============================================================
        // 0401 BATCH READ
        // ============================================================

        private byte[] ProcessBatchRead(
            byte[] request,
            ushort subCommand)
        {
            if (request.Length < 21)
            {
                return CreateErrorResponse(
                    request,
                    0xC051);
            }

            int address =
                ReadDeviceAddress(
                    request,
                    15);

            byte deviceCode =
                request[18];

            ushort points =
                ReadUInt16(
                    request,
                    19);

            if (points == 0)
            {
                return CreateErrorResponse(
                    request,
                    0xC051);
            }

            if (subCommand == 0x0000)
            {
                return ReadBatchWordUnits(
                    request,
                    deviceCode,
                    address,
                    points);
            }

            if (subCommand == 0x0001)
            {
                return ReadBatchBitUnits(
                    request,
                    deviceCode,
                    address,
                    points);
            }

            return CreateErrorResponse(
                request,
                0xC059);
        }

        // ============================================================
        // WORD UNIT READ
        // ============================================================

        private byte[] ReadBatchWordUnits(
            byte[] request,
            byte deviceCode,
            int startAddress,
            ushort points)
        {
            // ------------------------------------------
            // W
            // ------------------------------------------

            if (deviceCode == 0xB4)
            {
                // 정확히 points * 2 byte 반환
                byte[] data =
                    new byte[points * 2];

                int output = 0;

                for (int i = 0;
                     i < points;
                     i++)
                {
                    // 중요:
                    // 여기서는 Binding / UI를 절대 건드리지 않는다.
                    ushort value =
                        _memory.ReadWord(
                            startAddress + i);

                    data[output++] =
                        (byte)(
                            value & 0xFF);

                    data[output++] =
                        (byte)(
                            value >> 8);
                }

                return CreateSuccessResponse(
                    request,
                    data);
            }

            // ------------------------------------------
            // B를 WORD 단위로 읽기
            // ------------------------------------------

            if (deviceCode == 0xA0)
            {
                byte[] data =
                    new byte[points * 2];

                int output = 0;

                for (int wordIndex = 0;
                     wordIndex < points;
                     wordIndex++)
                {
                    ushort value = 0;

                    int bitAddress =
                        startAddress +
                        (wordIndex * 16);

                    for (int bit = 0;
                         bit < 16;
                         bit++)
                    {
                        if (_memory.ReadBit(
                            bitAddress + bit))
                        {
                            value |=
                                (ushort)(1 << bit);
                        }
                    }

                    data[output++] =
                        (byte)(
                            value & 0xFF);

                    data[output++] =
                        (byte)(
                            value >> 8);
                }

                return CreateSuccessResponse(
                    request,
                    data);
            }

            return CreateErrorResponse(
                request,
                0xC051);
        }

        // ============================================================
        // BIT UNIT READ
        // ============================================================

        private byte[] ReadBatchBitUnits(
            byte[] request,
            byte deviceCode,
            int startAddress,
            ushort points)
        {
            if (deviceCode != 0xA0)
            {
                return CreateErrorResponse(
                    request,
                    0xC051);
            }

            int byteCount =
                (points + 1) / 2;

            byte[] data =
                new byte[byteCount];

            int output = 0;

            for (int i = 0;
                 i < points;
                 i += 2)
            {
                byte value = 0;

                // 첫 point = upper nibble
                if (_memory.ReadBit(
                    startAddress + i))
                {
                    value |= 0x10;
                }

                // 두 번째 = lower nibble
                if (i + 1 < points &&
                    _memory.ReadBit(
                        startAddress + i + 1))
                {
                    value |= 0x01;
                }

                data[output++] = value;
            }

            return CreateSuccessResponse(
                request,
                data);
        }

        // ============================================================
        // 1401 BATCH WRITE
        // ============================================================

        private byte[] ProcessBatchWrite(
            byte[] request,
            ushort subCommand)
        {
            if (request.Length < 21)
            {
                return CreateErrorResponse(
                    request,
                    0xC051);
            }

            int address =
                ReadDeviceAddress(
                    request,
                    15);

            byte deviceCode =
                request[18];

            ushort points =
                ReadUInt16(
                    request,
                    19);

            if (points == 0)
            {
                return CreateErrorResponse(
                    request,
                    0xC051);
            }

            if (subCommand == 0x0000)
            {
                return WriteBatchWordUnits(
                    request,
                    deviceCode,
                    address,
                    points);
            }

            if (subCommand == 0x0001)
            {
                return WriteBatchBitUnits(
                    request,
                    deviceCode,
                    address,
                    points);
            }

            return CreateErrorResponse(
                request,
                0xC059);
        }

        // ============================================================
        // WORD UNIT WRITE
        // ============================================================

        private byte[] WriteBatchWordUnits(
            byte[] request,
            byte deviceCode,
            int startAddress,
            ushort points)
        {
            const int dataOffset = 21;

            int requiredLength =
                dataOffset +
                (points * 2);

            if (request.Length <
                requiredLength)
            {
                return CreateErrorResponse(
                    request,
                    0xC051);
            }

            // ------------------------------------------
            // W
            // ------------------------------------------

            if (deviceCode == 0xB4)
            {
                int offset =
                    dataOffset;

                // 1.
                // 먼저 Memory만 빠르게 갱신
                for (int i = 0;
                     i < points;
                     i++)
                {
                    ushort value =
                        (ushort)(
                            request[offset] |
                            (request[offset + 1] << 8));

                    _memory.WriteWord(
                        startAddress + i,
                        value);

                    offset += 2;
                }

                // 2.
                // 실제 Binding이 있는 주소만
                // UI Property로 전달.
                //
                // RefreshBindingsInRange 내부에서
                // cache를 사용해야 한다.
                _bindingManager
                    .RefreshBindingsInRange(
                        startAddress,
                        points);

                return CreateSuccessResponse(
                    request,
                    Array.Empty<byte>());
            }

            // ------------------------------------------
            // B WORD WRITE
            // ------------------------------------------

            if (deviceCode == 0xA0)
            {
                int offset =
                    dataOffset;

                for (int wordIndex = 0;
                     wordIndex < points;
                     wordIndex++)
                {
                    ushort word =
                        (ushort)(
                            request[offset] |
                            (request[offset + 1] << 8));

                    offset += 2;

                    int bitAddress =
                        startAddress +
                        (wordIndex * 16);

                    for (int bit = 0;
                         bit < 16;
                         bit++)
                    {
                        bool value =
                            (word &
                             (1 << bit)) != 0;

                        // Memory 우선
                        _memory.WriteBit(
                            bitAddress + bit,
                            value);
                    }
                }

                // B 바인딩까지 갱신해야 한다면
                // 별도 Range refresh가 필요하지만
                // Read 성능에는 영향을 주지 않음.

                return CreateSuccessResponse(
                    request,
                    Array.Empty<byte>());
            }

            return CreateErrorResponse(
                request,
                0xC051);
        }

        // ============================================================
        // BIT UNIT WRITE
        // ============================================================

        private byte[] WriteBatchBitUnits(
            byte[] request,
            byte deviceCode,
            int startAddress,
            ushort points)
        {
            if (deviceCode != 0xA0)
            {
                return CreateErrorResponse(
                    request,
                    0xC051);
            }

            const int dataOffset = 21;

            int byteCount =
                (points + 1) / 2;

            if (request.Length <
                dataOffset + byteCount)
            {
                return CreateErrorResponse(
                    request,
                    0xC051);
            }

            for (int i = 0;
                 i < points;
                 i++)
            {
                byte packed =
                    request[
                        dataOffset +
                        (i / 2)];

                bool value;

                if ((i & 1) == 0)
                {
                    value =
                        (packed & 0x10) != 0;
                }
                else
                {
                    value =
                        (packed & 0x01) != 0;
                }

                // Memory + 해당 Binding만 갱신
                _bindingManager.WriteBit(
                    startAddress + i,
                    value);
            }

            return CreateSuccessResponse(
                request,
                Array.Empty<byte>());
        }

        // ============================================================
        // 0403 RANDOM READ
        // ============================================================

        private byte[] ProcessRandomRead(
            byte[] request)
        {
            if (request.Length < 17)
            {
                return CreateErrorResponse(
                    request,
                    0xC051);
            }

            // MC 3E binary 0403:
            // byte 15 = word access count
            // byte 16 = double-word access count

            int wordCount =
                request[15];

            int doubleWordCount =
                request[16];

            int offset = 17;

            int required =
                offset +
                ((wordCount +
                  doubleWordCount) * 4);

            if (request.Length < required)
            {
                return CreateErrorResponse(
                    request,
                    0xC051);
            }

            byte[] data =
                new byte[
                    (wordCount * 2) +
                    (doubleWordCount * 4)];

            int output = 0;

            // ------------------------------------------
            // WORD
            // ------------------------------------------

            for (int i = 0;
                 i < wordCount;
                 i++)
            {
                int address =
                    ReadDeviceAddress(
                        request,
                        offset);

                byte deviceCode =
                    request[offset + 3];

                offset += 4;

                ushort value;

                if (deviceCode == 0xB4)
                {
                    // Memory only
                    value =
                        _memory.ReadWord(
                            address);
                }
                else if (deviceCode == 0xA0)
                {
                    value = 0;

                    for (int bit = 0;
                         bit < 16;
                         bit++)
                    {
                        if (_memory.ReadBit(
                            address + bit))
                        {
                            value |=
                                (ushort)(1 << bit);
                        }
                    }
                }
                else
                {
                    return CreateErrorResponse(
                        request,
                        0xC051);
                }

                data[output++] =
                    (byte)(
                        value & 0xFF);

                data[output++] =
                    (byte)(
                        value >> 8);
            }

            // ------------------------------------------
            // DWORD
            // ------------------------------------------

            for (int i = 0;
                 i < doubleWordCount;
                 i++)
            {
                int address =
                    ReadDeviceAddress(
                        request,
                        offset);

                byte deviceCode =
                    request[offset + 3];

                offset += 4;

                uint value;

                if (deviceCode == 0xB4)
                {
                    // Memory only
                    value =
                        _memory.ReadDWord(
                            address);
                }
                else if (deviceCode == 0xA0)
                {
                    value = 0;

                    for (int bit = 0;
                         bit < 32;
                         bit++)
                    {
                        if (_memory.ReadBit(
                            address + bit))
                        {
                            value |=
                                1u << bit;
                        }
                    }
                }
                else
                {
                    return CreateErrorResponse(
                        request,
                        0xC051);
                }

                data[output++] =
                    (byte)(
                        value & 0xFF);

                data[output++] =
                    (byte)(
                        (value >> 8) & 0xFF);

                data[output++] =
                    (byte)(
                        (value >> 16) & 0xFF);

                data[output++] =
                    (byte)(
                        (value >> 24) & 0xFF);
            }

            return CreateSuccessResponse(
                request,
                data);
        }

        // ============================================================
        // HELPERS
        // ============================================================

        private static int ReadDeviceAddress(
            byte[] data,
            int offset)
        {
            return
                data[offset]
                | (data[offset + 1] << 8)
                | (data[offset + 2] << 16);
        }

        private static ushort ReadUInt16(
            byte[] data,
            int offset)
        {
            return (ushort)(
                data[offset]
                | (data[offset + 1] << 8));
        }

        // ============================================================
        // RESPONSE
        // ============================================================

        private static byte[] CreateSuccessResponse(
            byte[] request,
            byte[] data)
        {
            if (request.Length < 7)
                return Array.Empty<byte>();

            // 11 bytes =
            // subheader 2
            // network   1
            // pc        1
            // io        2
            // station   1
            // length    2
            // end code  2

            byte[] response =
                new byte[
                    11 +
                    data.Length];

            // Response subheader
            response[0] = 0xD0;
            response[1] = 0x00;

            // Routing information
            response[2] = request[2];
            response[3] = request[3];
            response[4] = request[4];
            response[5] = request[5];
            response[6] = request[6];

            // Response data length
            ushort length =
                (ushort)(
                    2 +
                    data.Length);

            response[7] =
                (byte)(
                    length & 0xFF);

            response[8] =
                (byte)(
                    length >> 8);

            // Completion code = success
            response[9] = 0x00;
            response[10] = 0x00;

            if (data.Length > 0)
            {
                Buffer.BlockCopy(
                    data,
                    0,
                    response,
                    11,
                    data.Length);
            }

            return response;
        }

        private static byte[] CreateErrorResponse(
            byte[] request,
            ushort errorCode)
        {
            if (request.Length < 7)
                return Array.Empty<byte>();

            byte[] response =
                new byte[11];

            response[0] = 0xD0;
            response[1] = 0x00;

            response[2] = request[2];
            response[3] = request[3];
            response[4] = request[4];
            response[5] = request[5];
            response[6] = request[6];

            // Completion code only = 2 bytes
            response[7] = 0x02;
            response[8] = 0x00;

            response[9] =
                (byte)(
                    errorCode & 0xFF);

            response[10] =
                (byte)(
                    errorCode >> 8);

            return response;
        }
    }
}



