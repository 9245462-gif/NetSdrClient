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

        // Fixed: Proper GetHashCode + Equals pair
        public override int GetHashCode()
        {
            // Using a simple but sufficient hash combination
            // No need for MD5 here — it's slow and unnecessary for equality
            return HashCode.Combine(_localEndPoint.Address, _localEndPoint.Port);
        }

        public override bool Equals(object? obj)
        {
            return obj is UdpClientWrapper other &&
                   _localEndPoint.Port == other._localEndPoint.Port &&
                   _localEndPoint.Address.Equals(other._localEndPoint.Address);
        }

        // Optional but recommended: implement == and != operators
        public static bool operator ==(UdpClientWrapper? left, UdpClientWrapper? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(UdpClientWrapper? left, UdpClientWrapper? right)
        {
            return !(left == right);
        }

        public override void Dispose()
        {
            base.Dispose();
            _udpClient?.Dispose();
        }
    }
}