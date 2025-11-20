using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetSdrClientApp.Networking
{
    public class TcpClientWrapper : NetworkClientBase, ITcpClient
    {
        private readonly string _host;
        private readonly int _port;
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;

        public bool Connected => _tcpClient != null && _tcpClient.Connected && _stream != null;

        public event EventHandler<byte[]>? MessageReceived;

        public TcpClientWrapper(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public void Connect()
        {
            if (Connected)
            {
                return;
            }

            SafeExecute(() =>
            {
                _tcpClient = new TcpClient();
                _tcpClient.Connect(_host, _port);
                _stream = _tcpClient.GetStream();
                
                StartCancellationToken();
                _ = StartListeningAsync();
            }, "TCP connection");
        }

        public void Disconnect()
        {
            if (Connected)
            {
                SafeExecute(() =>
                {
                    StopCancellationToken();
                    _stream?.Close();
                    _tcpClient?.Close();
                    
                    _tcpClient = null;
                    _stream = null;
                    
                    Console.WriteLine("Disconnected.");
                }, "TCP disconnection");
            }
            else
            {
                Console.WriteLine("No active connection to disconnect.");
            }
        }

        public async Task SendMessageAsync(byte[] data)
        {
            if (!Connected || _stream == null || !_stream.CanWrite)
                throw new InvalidOperationException("Not connected to a server.");

            await SafeExecuteAsync(async () =>
            {
                var hexString = data.Select(b => Convert.ToString(b, 16)).Aggregate((l, r) => $"{l} {r}");
                Console.WriteLine($"Message sent: {hexString}");
                await _stream.WriteAsync(data, 0, data.Length);
            }, "TCP message sending");
        }

        public async Task SendMessageAsync(string str)
        {
            var data = Encoding.UTF8.GetBytes(str);
            await SendMessageAsync(data);
        }

        private async Task StartListeningAsync()
        {
            if (!Connected || _stream == null || !_stream.CanRead)
                throw new InvalidOperationException("Not connected to a server.");

            await SafeExecuteAsync(async () =>
            {
                Console.WriteLine("Starting listening for incoming messages.");

                var buffer = new byte[8194];
                while (!_cts!.Token.IsCancellationRequested)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
                    if (bytesRead > 0)
                    {
                        var message = buffer.AsSpan(0, bytesRead).ToArray();
                        MessageReceived?.Invoke(this, message);
                    }
                }
            }, "TCP listening");
        }

        public override void Dispose()
        {
            base.Dispose();
            _stream?.Dispose();
            _tcpClient?.Dispose();
        }
    }
}