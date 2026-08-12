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

            byte deviceCode =
                request[18];

            ushort points =
                BitConverter.ToUInt16(request, 19);

            if (command == 0x0401)
            {
                if (deviceCode == 0xA0) // B
                {
                    return ReadBitBatch(
                        request,
                        deviceNumber,
                        points);
                }

                if (deviceCode == 0xB4) // W
                {
                    return ReadWordBatch(
                        request,
                        deviceNumber,
                        points);
                }

                return CreateErrorResponse(
                    request,
                    0xC051);
            }

            // 0403
            if (command == 0x0403)
            {
                return ProcessRandomRead(request);
            }

            return CreateErrorResponse(
                request,
                0xC059);
        }

        private string GetPlcAddress(byte deviceCode, int address)
        {
            return deviceCode switch
            {
                0xA0 => $"B{address:X}",
                0xB4 => $"W{address:X}",

                _ => throw new NotSupportedException(
                    $"Unsupported device code: 0x{deviceCode:X2}")
            };
        }

        private byte[] ReadWordBatch(byte[] request, int startAddress, ushort points)
        {
            if (points == 0)
                return CreateErrorResponse(request, 0xC051);

            var data = new List<byte>();

            for (int i = 0; i < points; i++)
            {
                int address =
                    startAddress + i;

                string plcAddress =
                    $"W{address:X}";

                object? value =
                    _bindingManager.GetValue(plcAddress);

                ushort word = 0;

                if (value != null)
                {
                    word = Convert.ToUInt16(value);
                }

                data.Add((byte)(word & 0xFF));
                data.Add((byte)(word >> 8));
            }

            return CreateWordResponse(
                request,
                data.ToArray());
        }

        private byte[] CreateWordResponse(byte[] request, byte[] data)
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
                (ushort)(2 + data.Length);

            ms.WriteByte(
                (byte)(dataLength & 0xFF));

            ms.WriteByte(
                (byte)(dataLength >> 8));

            // Completion Code
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);

            ms.Write(
                data,
                0,
                data.Length);

            return ms.ToArray();
        }

        private byte[] ReadBitBatch(byte[] request, int startAddress, ushort points)
        {
            if (points == 0)
                return CreateErrorResponse(request, 0xC051);

            var data = new List<byte>();

            // 1 point 요청이어도 16개의 B를 하나의 word로 읽음
            for (int wordIndex = 0;
                 wordIndex < points;
                 wordIndex++)
            {
                ushort word = 0;

                for (int bit = 0; bit < 16; bit++)
                {
                    int address =
                        startAddress +
                        wordIndex * 16 +
                        bit;

                    string plcAddress =
                        $"B{address:X}";

                    object? value =
                        _bindingManager.GetValue(plcAddress);

                    bool on =
                        value != null &&
                        Convert.ToBoolean(value);

                    if (on)
                        word |= (ushort)(1 << bit);
                }

                data.Add((byte)(word & 0xFF));
                data.Add((byte)(word >> 8));
            }

            return CreateWordResponse(
                request,
                data.ToArray());
        }

        private byte[] ProcessRandomRead(byte[] request)
        {
            // 0403 request data
            //
            // 15-16 : number of word access points
            // 17-18 : number of double word access points
            // 이후  : word device list
            //         double word device list

            Debug.WriteLine($"0403 Length = {request.Length}");
            for (int i = 0; i < request.Length; i++)
            {
                Debug.WriteLine(
                $"[{i}] = {request[i]:X2}");
            }

            if (request.Length < 19)
                return CreateErrorResponse(request, 0xC051);

            ushort wordCount =
                BitConverter.ToUInt16(request, 15);

            ushort doubleWordCount =
                BitConverter.ToUInt16(request, 17);

            int offset = 19;

            var data = new List<byte>();

            // -------------------------
            // Word access
            // -------------------------
            for (int i = 0; i < wordCount; i++)
            {
                if (offset + 4 > request.Length)
                    return CreateErrorResponse(request, 0xC051);

                int address =
                    request[offset]
                    | (request[offset + 1] << 8)
                    | (request[offset + 2] << 16);

                byte deviceCode =
                    request[offset + 3];

                offset += 4;

                string plcAddress =
                    GetPlcAddress(
                        deviceCode,
                        address);

                object? value =
                    _bindingManager.GetValue(plcAddress);

                Debug.WriteLine(
                    $"0403 READ {plcAddress} = {value}");

                ushort word = 0;

                if (value != null)
                {
                    if (deviceCode == 0xA0)
                    {
                        // B device
                        //
                        // true  -> 0001
                        // false -> 0000

                        if (Convert.ToBoolean(value))
                            word = 1;
                    }
                    else
                    {
                        word = Convert.ToUInt16(value);
                    }
                }

                data.Add(
                    (byte)(word & 0xFF));

                data.Add(
                    (byte)((word >> 8) & 0xFF));
            }

            // -------------------------
            // Double word access
            // -------------------------
            for (int i = 0; i < doubleWordCount; i++)
            {
                if (offset + 4 > request.Length)
                    return CreateErrorResponse(request, 0xC051);

                int address =
                    request[offset]
                    | (request[offset + 1] << 8)
                    | (request[offset + 2] << 16);

                byte deviceCode =
                    request[offset + 3];

                offset += 4;

                string plcAddress =
                    GetPlcAddress(
                        deviceCode,
                        address);

                object? value =
                    _bindingManager.GetValue(plcAddress);

                Debug.WriteLine(
                    $"0403 DREAD {plcAddress} = {value}");

                uint dword = 0;

                if (value != null)
                {
                    dword =
                        Convert.ToUInt32(value);
                }

                data.Add(
                    (byte)(dword & 0xFF));

                data.Add(
                    (byte)((dword >> 8) & 0xFF));

                data.Add(
                    (byte)((dword >> 16) & 0xFF));

                data.Add(
                    (byte)((dword >> 24) & 0xFF));
            }

            return CreateWordResponse(
                request,
                data.ToArray());
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
