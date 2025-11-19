using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NetSdrClientApp.Networking
{
    public class UdpClientWrapper : NetworkClientBase, IUdpClient
    {
        private readonly IPEndPoint _localEndPoint;
        private UdpClient? _udpClient;

        public event EventHandler<byte[]>? MessageReceived;

        public UdpClientWrapper(int port)
        {
            _localEndPoint = new IPEndPoint(IPAddress.Any, port);
        }

        public async Task StartListeningAsync()
        {
            await SafeExecuteAsync(async () =>
            {
                StartCancellationToken();
                _udpClient = new UdpClient(_localEndPoint);
                
                Console.WriteLine("Start listening for UDP messages...");

                while (!_cts!.Token.IsCancellationRequested)
                {
                    var result = await _udpClient.ReceiveAsync(_cts.Token);
                    MessageReceived?.Invoke(this, result.Buffer);
                    Console.WriteLine($"Received from {result.RemoteEndPoint}");
                }
            }, "UDP listening");
        }

        public void StopListening()
        {
            SafeExecute(() =>
            {
                StopCancellationToken();
                _udpClient?.Close();
                Console.WriteLine("Stopped listening for UDP messages.");
            }, "UDP stopping");
        }

        public void Exit() => StopListening();

        public override int GetHashCode()
        {
            var payload = $"{nameof(UdpClientWrapper)}|{_localEndPoint.Address}|{_localEndPoint.Port}";

            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return BitConverter.ToInt32(hash, 0);
        }

        public override void Dispose()
        {
            base.Dispose();
            _udpClient?.Dispose();
        }
    }
}