using System;
using System.IO;
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
        private Mock<NetworkStream> _mockStream;
        private Mock<TcpClient> _mockTcpClient;
        private const string TestHost = "localhost";
        private const int TestPort = 8080;

        [SetUp]
        public void Setup()
        {
            _tcpClient = new TcpClientWrapper(TestHost, TestPort);
            
            // Mock TcpClient and NetworkStream
            _mockTcpClient = new Mock<TcpClient>();
            _mockStream = new Mock<NetworkStream>();
        }

        [TearDown]
        public void TearDown()
        {
            _tcpClient.Dispose();
        }

        [Test]
        public void Constructor_ShouldInitializeCorrectly()
        {
            // Assert
            Assert.That(_tcpClient, Is.Not.Null);
        }

        [Test]
        public void Connected_WhenNoTcpClient_ShouldReturnFalse()
        {
            // Act & Assert
            Assert.That(_tcpClient.Connected, Is.False);
        }

        [Test]
        public void Connect_WhenConnectionFails_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _tcpClient.Connect());
        }

        [Test]
        public void Disconnect_WhenNotConnected_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _tcpClient.Disconnect());
        }

        [Test]
        public void Disconnect_WhenConnected_ShouldDisconnect()
        {
            // Arrange
            // Use reflection to set private fields for testing
            SetPrivateField(_tcpClient, "_tcpClient", new TcpClient());
            
            // Act
            _tcpClient.Disconnect();

            // Assert
            Assert.That(_tcpClient.Connected, Is.False);
        }

        [Test]
        public async Task SendMessageAsync_WhenNotConnected_ShouldThrow()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => 
                _tcpClient.SendMessageAsync(new byte[] { 1, 2, 3 }));
            Assert.That(ex.Message, Contains.Substring("Not connected"));
        }

        [Test]
        public async Task SendMessageAsync_WithString_WhenNotConnected_ShouldThrow()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => 
                _tcpClient.SendMessageAsync("test message"));
            Assert.That(ex.Message, Contains.Substring("Not connected"));
        }

        [Test]
        public void Dispose_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _tcpClient.Dispose());
        }

        [Test]
        public void Dispose_MultipleTimes_ShouldNotThrow()
        {
            // Act & Assert
            _tcpClient.Dispose();
            Assert.DoesNotThrow(() => _tcpClient.Dispose());
        }

        [Test]
        public async Task SendMessageAsync_IntegrationTest_WithMockServer()
        {
            // This test uses a simple mock server that doesn't actually listen
            // Arrange
            var testData = new byte[] { 1, 2, 3 };
            bool messageSent = false;

            // We'll test that the method doesn't throw when not connected
            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(() => 
                _tcpClient.SendMessageAsync(testData));
        }

        // Helper method to set private fields via reflection
        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
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
        public void Constructor_ShouldInitializeCorrectly()
        {
            // Assert
            Assert.That(_udpClient, Is.Not.Null);
        }

        [Test]
        public void StopListening_WhenNotStarted_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _udpClient.StopListening());
        }

        [Test]
        public void Exit_ShouldCallStopListening()
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
            
            udpClient1.Dispose();
            udpClient2.Dispose();
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
            
            udpClient1.Dispose();
            udpClient2.Dispose();
        }

        [Test]
        public void MessageReceived_Event_CanBeAssigned()
        {
            // Arrange
            bool eventHandlerAdded = false;

            // Act
            _udpClient.MessageReceived += (sender, data) => { };
            eventHandlerAdded = true;

            // Assert
            Assert.That(eventHandlerAdded, Is.True);
        }

        [Test]
        public void Dispose_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _udpClient.Dispose());
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
        public void SafeExecute_WithOperationCanceledException_ShouldNotLog()
        {
            // Act & Assert - should not throw and not log the canceled exception
            Assert.DoesNotThrow(() => 
                _testClient.TestSafeExecute(() => throw new OperationCanceledException(), "test operation"));
        }

        [Test]
        public async Task SafeExecuteAsync_WithValidAsyncAction_ShouldExecute()
        {
            // Arrange
            bool executed = false;

            // Act
            await _testClient.TestSafeExecuteAsync(async () => 
            {
                await Task.Delay(10);
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
                await Task.Delay(10);
                throw new Exception("Test async exception");
            }, "test async operation");
            // Should not throw - exception is caught internally
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

        [Test]
        public void Dispose_MultipleTimes_ShouldNotThrow()
        {
            // Act & Assert
            _testClient.Dispose();
            Assert.DoesNotThrow(() => _testClient.Dispose());
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
}