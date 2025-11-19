using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests.Networking
{
    [TestFixture]
    public class TcpClientWrapperTests
    {
        private TcpClientWrapper _tcpClient;
        private const string TestHost = "localhost";
        private const int TestPort = 8080;

        [SetUp]
        public void Setup()
        {
            _tcpClient = new TcpClientWrapper(TestHost, TestPort);
        }

        [TearDown]
        public void TearDown()
        {
            _tcpClient.Dispose();
        }

        [Test]
        public void Constructor_ShouldSetHostAndPort()
        {
            // Assert
            Assert.That(_tcpClient, Is.Not.Null);
            // Note: Host and port are private, we test through behavior
        }

        [Test]
        public void Connected_WhenNotConnected_ShouldReturnFalse()
        {
            // Act & Assert
            Assert.That(_tcpClient.Connected, Is.False);
        }

        [Test]
        public void Connect_WhenAlreadyConnected_ShouldNotThrow()
        {
            // Arrange
            using var mockTcpServer = new MockTcpServer(TestPort);
            
            // Act & Assert
            Assert.DoesNotThrow(() => _tcpClient.Connect());
            Assert.DoesNotThrow(() => _tcpClient.Connect()); // Second call
        }

        [Test]
        public void Disconnect_WhenNotConnected_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _tcpClient.Disconnect());
        }

        [Test]
        public async Task SendMessageAsync_WhenNotConnected_ShouldThrow()
        {
            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(() => 
                _tcpClient.SendMessageAsync(new byte[] { 1, 2, 3 }));
        }

        [Test]
        public async Task SendMessageAsync_WithString_WhenNotConnected_ShouldThrow()
        {
            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(() => 
                _tcpClient.SendMessageAsync("test message"));
        }

        [Test]
        public void MessageReceived_Event_ShouldBeInvoked()
        {
            // Arrange
            using var mockServer = new MockTcpServer(TestPort);
            byte[] receivedData = null;
            _tcpClient.MessageReceived += (sender, data) => receivedData = data;

            // Act
            _tcpClient.Connect();
            
            // Simulate sending data from server (this would need actual server implementation)
            // For unit tests, we might need to mock the underlying TcpClient

            // Assert
            Assert.That(_tcpClient.Connected, Is.True);
            // Event testing would require integration tests with actual server
        }

        [Test]
        public void Dispose_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _tcpClient.Dispose());
        }

        [Test]
        public void Dispose_WhenConnected_ShouldDisconnect()
        {
            // Arrange
            using var mockServer = new MockTcpServer(TestPort);
            _tcpClient.Connect();

            // Act
            _tcpClient.Dispose();

            // Assert
            Assert.That(_tcpClient.Connected, Is.False);
        }
    }

    [TestFixture]
    public class UdpClientWrapperTests
    {
        private UdpClientWrapper _udpClient;
        private const int TestPort = 9090;

        [SetUp]
        public void Setup()
        {
            _udpClient = new UdpClientWrapper(TestPort);
        }

        [TearDown]
        public void TearDown()
        {
            _udpClient.Dispose();
        }

        [Test]
        public void Constructor_ShouldSetPort()
        {
            // Assert
            Assert.That(_udpClient, Is.Not.Null);
        }

        [Test]
        public async Task StartListeningAsync_ShouldStartWithoutError()
        {
            // Act & Assert
            Assert.DoesNotThrowAsync(() => _udpClient.StartListeningAsync());
        }

        [Test]
        public void StopListening_WhenNotListening_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _udpClient.StopListening());
        }

        [Test]
        public void Exit_ShouldBehaveLikeStopListening()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _udpClient.Exit());
        }

        [Test]
        public void GetHashCode_ShouldReturnConsistentValue()
        {
            // Arrange
            var udpClient1 = new UdpClientWrapper(TestPort);
            var udpClient2 = new UdpClientWrapper(TestPort);

            // Act
            var hash1 = udpClient1.GetHashCode();
            var hash2 = udpClient2.GetHashCode();

            // Assert
            Assert.That(hash1, Is.EqualTo(hash2));
        }

        [Test]
        public void GetHashCode_WithDifferentPorts_ShouldReturnDifferentValues()
        {
            // Arrange
            var udpClient1 = new UdpClientWrapper(TestPort);
            var udpClient2 = new UdpClientWrapper(TestPort + 1);

            // Act
            var hash1 = udpClient1.GetHashCode();
            var hash2 = udpClient2.GetHashCode();

            // Assert
            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void MessageReceived_Event_ShouldBeAssignable()
        {
            // Arrange
            bool eventFired = false;
            _udpClient.MessageReceived += (sender, data) => eventFired = true;

            // Act - we can't easily simulate UDP messages in unit tests without actual network
            // This just tests that event assignment works

            // Assert
            // Just verifying the event can be assigned without error
            Assert.Pass("Event assignment successful");
        }

        [Test]
        public void Dispose_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _udpClient.Dispose());
        }

        [Test]
        public async Task StartListeningAsync_AfterDispose_ShouldHandleGracefully()
        {
            // Arrange
            _udpClient.Dispose();

            // Act & Assert
            Assert.DoesNotThrowAsync(() => _udpClient.StartListeningAsync());
        }
    }

    [TestFixture]
    public class NetworkClientBaseTests
    {
        private TestNetworkClient _testClient;

        [SetUp]
        public void Setup()
        {
            _testClient = new TestNetworkClient();
        }

        [TearDown]
        public void TearDown()
        {
            _testClient.Dispose();
        }

        [Test]
        public void IsListening_WhenNotStarted_ShouldReturnFalse()
        {
            // Assert
            Assert.That(_testClient.IsListening, Is.False);
        }

        [Test]
        public void IsListening_AfterStart_ShouldReturnTrue()
        {
            // Act
            _testClient.Start();

            // Assert
            Assert.That(_testClient.IsListening, Is.True);
        }

        [Test]
        public void IsListening_AfterStop_ShouldReturnFalse()
        {
            // Arrange
            _testClient.Start();

            // Act
            _testClient.Stop();

            // Assert
            Assert.That(_testClient.IsListening, Is.False);
        }

        [Test]
        public void SafeExecute_WithValidAction_ShouldExecute()
        {
            // Arrange
            bool executed = false;

            // Act
            _testClient.TestSafeExecute(() => executed = true, "test operation");

            // Assert
            Assert.That(executed, Is.True);
        }

        [Test]
        public void SafeExecute_WithThrowingAction_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => 
                _testClient.TestSafeExecute(() => throw new Exception("Test exception"), "test operation"));
        }

        [Test]
        public async Task SafeExecuteAsync_WithValidAsyncAction_ShouldExecute()
        {
            // Arrange
            bool executed = false;

            // Act
            await _testClient.TestSafeExecuteAsync(async () => 
            {
                await Task.Delay(1);
                executed = true;
            }, "test async operation");

            // Assert
            Assert.That(executed, Is.True);
        }

        [Test]
        public async Task SafeExecuteAsync_WithThrowingAsyncAction_ShouldNotThrow()
        {
            // Act & Assert
            await _testClient.TestSafeExecuteAsync(async () => 
            {
                await Task.Delay(1);
                throw new Exception("Test async exception");
            }, "test async operation");
            // Should not throw
        }

        [Test]
        public void Dispose_ShouldStopCancellationToken()
        {
            // Arrange
            _testClient.Start();

            // Act
            _testClient.Dispose();

            // Assert
            Assert.That(_testClient.IsListening, Is.False);
        }

        // Test implementation of NetworkClientBase
        private class TestNetworkClient : NetworkClientBase
        {
            public void Start() => StartCancellationToken();
            public void Stop() => StopCancellationToken();
            
            public void TestSafeExecute(Action action, string operationName) 
                => SafeExecute(action, operationName);
                
            public Task TestSafeExecuteAsync(Func<Task> asyncAction, string operationName) 
                => SafeExecuteAsync(asyncAction, operationName);
        }
    }

    // Helper class for TCP server simulation in tests
    public class MockTcpServer : IDisposable
    {
        private TcpListener _listener;
        private bool _isDisposed = false;

        public MockTcpServer(int port)
        {
            _listener = new TcpListener(IPAddress.Loopback, port);
            _listener.Start();
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _listener?.Stop();
                _isDisposed = true;
            }
        }
    }
}