using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace MaterialControlSimulator.Plc
{
    public class McProtocolServer
    {
        private readonly PlcBindingManager _bindingManager;

        private TcpListener? _listener;
        private CancellationTokenSource? _cts;

        public bool IsRunning => _listener != null;

        public McProtocolServer(
            PlcBindingManager bindingManager)
        {
            _bindingManager = bindingManager;
        }

        public async Task StartAsync(int port = 5000)
        {
            if (IsRunning)
                return;

            _cts = new CancellationTokenSource();

            _listener = new TcpListener(
                IPAddress.Any,
                port);

            _listener.Start();

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var client =
                        await _listener.AcceptTcpClientAsync(
                            _cts.Token);

                    _ = HandleClientAsync(client);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // 로그
                }
            }
        }

        private async Task HandleClientAsync(
            TcpClient client)
        {
            using (client)
            {
                var stream = client.GetStream();

                var buffer = new byte[4096];

                while (client.Connected)
                {
                    int length;

                    try
                    {
                        length = await stream.ReadAsync(
                            buffer,
                            _cts?.Token ?? CancellationToken.None);
                    }
                    catch
                    {
                        break;
                    }

                    if (length <= 0)
                        break;

                    var request =
                        buffer[..length];

                    Debug.WriteLine($"MC RX [{length}] : " + BitConverter.ToString(request));

                    var response =
                        ProcessRequest(request);

                    Debug.WriteLine($"MC TX [{response.Length}] : " +BitConverter.ToString(response));

                    if (response.Length > 0)
                    {
                        await stream.WriteAsync(
                            response,
                            _cts?.Token ??
                            CancellationToken.None);
                    }
                }
            }
        }

        private byte[] ProcessRequest(byte[] request)
        {
            if (request.Length < 21)
                return Array.Empty<byte>();

            if (request[0] != 0x50 ||
                request[1] != 0x00)
                return Array.Empty<byte>();

            ushort command =
                BitConverter.ToUInt16(request, 11);

            ushort subCommand =
                BitConverter.ToUInt16(request, 13);

            int deviceNumber =
                request[15]
                | (request[16] << 8)
                | (request[17] << 16);

            byte deviceCode = request[18];

            ushort points =
                BitConverter.ToUInt16(request, 19);

            // 0401 Batch Read
            if (command == 0x0401)
            {
                // B 디바이스
                if (deviceCode == 0xA0)
                {
                    if (subCommand == 0x0000)
                    {
                        return ReadBit16(
                            request,
                            deviceNumber,
                            points);
                    }

                    if (subCommand == 0x0001)
                    {
                        return ReadBit1(
                            request,
                            deviceNumber,
                            points);
                    }
                }

                return CreateErrorResponse(
                    request,
                    0xC051);
            }

            // 0403은 일단 로그만 확인
            if (command == 0x0403)
            {
                return ProcessRandomRead(request);
            }

            return CreateErrorResponse(
                request,
                0xC059);
        }

        #region MyRegion
        private byte[] ReadBit16(
            byte[] request,
            int startAddress,
            ushort points)
        {
            if (points == 0)
                return CreateErrorResponse(request, 0xC051);

            var data = new List<byte>();

            for (int wordIndex = 0; wordIndex < points; wordIndex++)
            {
                ushort word = 0;

                // 1 word = 16 bit devices
                for (int bit = 0; bit < 16; bit++)
                {
                    int address =
                        startAddress +
                        (wordIndex * 16) +
                        bit;

                    string plcAddress =
                        $"B{address:X}";

                    object? value =
                        _bindingManager.GetValue(plcAddress);

                    bool on =
                        value != null &&
                        Convert.ToBoolean(value);

                    Debug.WriteLine(
                        $"MC READ {plcAddress} = {on}");

                    if (on)
                    {
                        word |= (ushort)(1 << bit);
                    }
                }

                // MC Binary = little endian
                data.Add((byte)(word & 0xFF));
                data.Add((byte)((word >> 8) & 0xFF));
            }

            return CreateWordResponse(
                request,
                data.ToArray());
        }

        private byte[] CreateWordResponse(
            byte[] request,
            byte[] data)
        {
            using var ms = new MemoryStream();

            // Response subheader
            ms.WriteByte(0xD0);
            ms.WriteByte(0x00);

            // Network
            ms.WriteByte(request[2]);

            // PC No.
            ms.WriteByte(request[3]);

            // Request I/O
            ms.WriteByte(request[4]);
            ms.WriteByte(request[5]);

            // Station
            ms.WriteByte(request[6]);

            // Data length
            // Completion Code 2byte + actual data
            ushort dataLength =
                (ushort)(2 + data.Length);

            ms.WriteByte(
                (byte)(dataLength & 0xFF));

            ms.WriteByte(
                (byte)((dataLength >> 8) & 0xFF));

            // Completion Code = 0000
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);

            // Data
            ms.Write(
                data,
                0,
                data.Length);

            return ms.ToArray();
        }


        private byte[] ReadBit1(
    byte[] request,
    int startAddress,
    ushort points)
        {
            if (points == 0)
            {
                return CreateErrorResponse(
                    request,
                    0xC051);
            }

            var result = new List<byte>();

            for (int i = 0; i < points; i++)
            {
                string address =
                    $"B{(startAddress + i):X}";

                var value =
                    _bindingManager.GetValue(address);

                result.Add(
                    value != null &&
                    Convert.ToBoolean(value)
                        ? (byte)0x01
                        : (byte)0x00);
            }

            return CreateBit1Response(
                request,
                result);
        }
        private byte[] CreateBit1Response(
    byte[] request,
    List<byte> values)
        {
            using var ms = new MemoryStream();

            ms.WriteByte(0xD0);
            ms.WriteByte(0x00);

            ms.WriteByte(request[2]);
            ms.WriteByte(request[3]);

            ms.WriteByte(request[4]);
            ms.WriteByte(request[5]);

            ms.WriteByte(request[6]);

            ushort dataLength =
                (ushort)(2 + values.Count);

            ms.WriteByte(
                (byte)(dataLength & 0xFF));

            ms.WriteByte(
                (byte)((dataLength >> 8) & 0xFF));

            // Completion code
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);

            foreach (var value in values)
                ms.WriteByte(value);

            return ms.ToArray();
        }
        #endregion

        private byte[] ProcessRandomRead(byte[] request)
        {
            // 0403 Random Read
            //
            // 요청:
            // Command    : 0403
            // Subcommand : 0000
            //
            // Random Read는
            // word device / bit device를 각각 지정할 수 있다.

            if (request.Length < 15)
                return Array.Empty<byte>();

            ushort subCommand =
                BitConverter.ToUInt16(request, 13);

            if (subCommand != 0x0000)
            {
                return CreateErrorResponse(
                    request,
                    0xC05C);
            }

            // ------------------------------------------------
            // 0403 요청 데이터
            //
            // 이후:
            // Number of word devices
            // Number of double-word devices
            // ...
            // ------------------------------------------------

            int offset = 15;

            if (request.Length < offset + 2)
                return CreateErrorResponse(
                    request,
                    0xC051);

            byte wordCount = request[offset];
            byte dWordCount = request[offset + 1];

            offset += 2;

            var values = new List<ushort>();

            // ------------------------------------------------
            // Word devices
            // ------------------------------------------------

            for (int i = 0; i < wordCount; i++)
            {
                if (request.Length < offset + 4)
                {
                    return CreateErrorResponse(
                        request,
                        0xC051);
                }

                int deviceNumber =
                    request[offset]
                    | (request[offset + 1] << 8)
                    | (request[offset + 2] << 16);

                byte deviceCode =
                    request[offset + 3];

                offset += 4;

                string address =
                    deviceCode switch
                    {
                        0xA0 => $"B{deviceNumber:X}",
                        0xB4 => $"W{deviceNumber:X}",

                        _ => string.Empty
                    };

                if (string.IsNullOrEmpty(address))
                {
                    return CreateErrorResponse(
                        request,
                        0xC051);
                }

                object? value =
                    _bindingManager.GetValue(address);

                ushort wordValue = 0;

                if (value != null)
                {
                    wordValue =
                        Convert.ToUInt16(value);
                }

                values.Add(wordValue);
            }

            // ------------------------------------------------
            // Double Word devices
            // ------------------------------------------------

            for (int i = 0; i < dWordCount; i++)
            {
                if (request.Length < offset + 4)
                {
                    return CreateErrorResponse(
                        request,
                        0xC051);
                }

                int deviceNumber =
                    request[offset]
                    | (request[offset + 1] << 8)
                    | (request[offset + 2] << 16);

                byte deviceCode =
                    request[offset + 3];

                offset += 4;

                string address =
                    deviceCode switch
                    {
                        0xA0 => $"B{deviceNumber:X}",
                        0xB4 => $"W{deviceNumber:X}",

                        _ => string.Empty
                    };

                if (string.IsNullOrEmpty(address))
                {
                    return CreateErrorResponse(
                        request,
                        0xC051);
                }

                object? value =
                    _bindingManager.GetValue(address);

                uint dwordValue = 0;

                if (value != null)
                {
                    dwordValue =
                        Convert.ToUInt32(value);
                }

                values.Add(
                    (ushort)(dwordValue & 0xFFFF));

                values.Add(
                    (ushort)((dwordValue >> 16) & 0xFFFF));
            }

            return CreateRandomReadResponse(
                request,
                values);
        }

        private byte[] CreateRandomReadResponse(    byte[] request,    List<ushort> values)
        {
            using var ms = new MemoryStream();

            // 3E response
            ms.WriteByte(0xD0);
            ms.WriteByte(0x00);

            // Network
            ms.WriteByte(request[2]);

            // PC
            ms.WriteByte(request[3]);

            // I/O
            ms.WriteByte(request[4]);
            ms.WriteByte(request[5]);

            // Station
            ms.WriteByte(request[6]);

            // Completion code + data
            ushort dataLength =
                (ushort)(2 + values.Count * 2);

            ms.WriteByte(
                (byte)(dataLength & 0xFF));

            ms.WriteByte(
                (byte)((dataLength >> 8) & 0xFF));

            // Completion code
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);

            foreach (ushort value in values)
            {
                ms.WriteByte(
                    (byte)(value & 0xFF));

                ms.WriteByte(
                    (byte)((value >> 8) & 0xFF));
            }

            return ms.ToArray();
        }

        private byte[] CreateErrorResponse(byte[] request, ushort errorCode)
        {
            using var ms = new MemoryStream();

            // Response subheader
            ms.WriteByte(0xD0);
            ms.WriteByte(0x00);

            // Network No.
            ms.WriteByte(request[2]);

            // PC No.
            ms.WriteByte(request[3]);

            // I/O
            ms.WriteByte(request[4]);
            ms.WriteByte(request[5]);

            // Station
            ms.WriteByte(request[6]);

            // Response data length
            // Completion code만 존재
            ms.WriteByte(0x02);
            ms.WriteByte(0x00);

            // Completion code
            ms.WriteByte(
                (byte)(errorCode & 0xFF));

            ms.WriteByte(
                (byte)((errorCode >> 8) & 0xFF));

            return ms.ToArray();
        }

        public void Stop()
        {
            _cts?.Cancel();

            _listener?.Stop();
            _listener = null;

            _cts?.Dispose();
            _cts = null;
        }
    }
}
