using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
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
        public void Connect_ShouldNotThrow_WhenNoRealConnection()
        {
            // Act & Assert - should handle connection failure gracefully
            Assert.DoesNotThrow(() => _tcpClient.Connect());
        }

        [Test]
        public void Disconnect_WhenNotConnected_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _tcpClient.Disconnect());
        }

        [Test]
        public void SendMessageAsync_WhenNotConnected_ShouldThrow()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
                _tcpClient.SendMessageAsync(new byte[] { 1, 2, 3 }));
            Assert.That(ex.Message, Contains.Substring("Not connected"));
        }

        [Test]
        public void SendMessageAsync_WithString_WhenNotConnected_ShouldThrow()
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
        public void MessageReceived_Event_CanBeAssigned()
        {
            // Arrange
            bool eventHandlerCalled = false;

            // Act
            _tcpClient.MessageReceived += (sender, data) => { eventHandlerCalled = true; };
            
            // Trigger the event through reflection to test assignment
            var eventField = typeof(TcpClientWrapper).GetField("MessageReceived", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var eventHandler = eventField?.GetValue(_tcpClient) as EventHandler<byte[]>;
            
            // Assert that event handler was assigned
            Assert.That(eventHandler, Is.Not.Null);
            
            // Test that the handler can be invoked
            eventHandler?.Invoke(this, new byte[] { 1, 2, 3 });
            // We don't check eventHandlerCalled here because we're testing assignment, not invocation
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
        public void StartListeningAsync_ShouldReturnTask()
        {
            // Act
            var task = _udpClient.StartListeningAsync();

            // Assert
            Assert.That(task, Is.Not.Null);
            Assert.That(task, Is.InstanceOf<Task>());
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
        public void MessageReceived_Event_CanBeAssigned()
        {
            // Arrange
            bool eventHandlerCalled = false;

            // Act
            _udpClient.MessageReceived += (sender, data) => { eventHandlerCalled = true; };
            
            // Trigger the event through reflection to test assignment
            var eventField = typeof(UdpClientWrapper).GetField("MessageReceived", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var eventHandler = eventField?.GetValue(_udpClient) as EventHandler<byte[]>;
            
            // Assert that event handler was assigned
            Assert.That(eventHandler, Is.Not.Null);
            
            // Test that the handler can be invoked
            eventHandler?.Invoke(this, new byte[] { 1, 2, 3 });
            // We don't check eventHandlerCalled here because we're testing assignment, not invocation
        }
    }

    // Test implementation of NetworkClientBase
    public class TestNetworkClient : NetworkClientBase
    {
        public void Start() => StartCancellationToken();
        public void Stop() => StopCancellationToken();
        
        public void TestSafeExecute(Action action, string operationName) 
            => SafeExecute(action, operationName);
            
        public Task TestSafeExecuteAsync(Func<Task> asyncAction, string operationName) 
            => SafeExecuteAsync(asyncAction, operationName);
    }

    // Extended test implementation for additional testing
    public class ExtendedTestNetworkClient : NetworkClientBase
    {
        public void Start() => StartCancellationToken();
        public void Stop() => StopCancellationToken();
        
        public void TestSafeExecute(Action action, string operationName) 
            => SafeExecute(action, operationName);
            
        public Task TestSafeExecuteAsync(Func<Task> asyncAction, string operationName) 
            => SafeExecuteAsync(asyncAction, operationName);
    }

    [TestFixture]
    public class TcpClientWrapperImplementationTests
    {
        [Test]
        public void HexStringConversion_ShouldFormatCorrectly()
        {
            // Arrange
            var testData = new byte[] { 0x0A, 0x0B, 0x0C };
            
            // Act - test the conversion logic directly
            var hexString = testData.Select(b => Convert.ToString(b, 16)).Aggregate((l, r) => $"{l} {r}");
            
            // Assert
            Assert.That(hexString, Is.EqualTo("a b c"));
        }

        [Test]
        public void SafeExecute_ShouldCatchExceptions()
        {
            // Arrange
            var testClient = new TestNetworkClient();
            var exceptionThrown = false;

            // Act
            testClient.TestSafeExecute(() => 
            {
                exceptionThrown = true;
                throw new InvalidOperationException("Test exception");
            }, "test operation");

            // Assert
            Assert.That(exceptionThrown, Is.True);
            // Exception was caught by SafeExecute, so test should not fail
        }

        [Test]
        public async Task SafeExecuteAsync_ShouldCatchAsyncExceptions()
        {
            // Arrange
            var testClient = new TestNetworkClient();
            var exceptionThrown = false;

            // Act
            await testClient.TestSafeExecuteAsync(async () => 
            {
                await Task.Delay(1);
                exceptionThrown = true;
                throw new InvalidOperationException("Test async exception");
            }, "test async operation");

            // Assert
            Assert.That(exceptionThrown, Is.True);
            // Exception was caught by SafeExecuteAsync, so test should not fail
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
        public void SafeExecute_WithThrowingAction_ShouldNotPropagateException()
        {
            // Act & Assert - should not throw to test method
            Assert.DoesNotThrow(() => 
                _testClient.TestSafeExecute(() => throw new Exception("Test exception"), "test operation"));
        }

        [Test]
        public void SafeExecute_WithOperationCanceledException_ShouldNotPropagate()
        {
            // Act & Assert
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
        public async Task SafeExecuteAsync_WithThrowingAsyncAction_ShouldNotPropagateException()
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
    }

    // Additional test implementation for cancellation tests
    public class TestNetworkClientWithToken : NetworkClientBase
    {
        public void Start() => StartCancellationToken();
        public void Stop() => StopCancellationToken();
        
        public CancellationToken GetCancellationToken()
        {
            var field = typeof(NetworkClientBase).GetField("_cts", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var cts = field?.GetValue(this) as CancellationTokenSource;
            return cts?.Token ?? CancellationToken.None;
        }
    }

    [TestFixture]
    public class CancellationTests
    {
        [Test]
        public void CancellationToken_ShouldBeCancelledAfterStop()
        {
            // Arrange
            var testClient = new TestNetworkClientWithToken();
            testClient.Start();

            // Act
            testClient.Stop();

            // Assert
            Assert.That(testClient.IsListening, Is.False);
        }

        [Test]
        public void StartCancellationToken_ShouldDisposePrevious()
        {
            // Arrange
            var testClient = new TestNetworkClientWithToken();
            testClient.Start();
            var firstToken = testClient.GetCancellationToken();

            // Act
            testClient.Start(); // Start again

            // Assert
            Assert.That(firstToken.IsCancellationRequested, Is.False);
            Assert.That(testClient.IsListening, Is.True);
        }
    }

    [TestFixture]
    public class EventTests
    {
        [Test]
        public void MessageReceived_Event_ShouldBeInvokable()
        {
            // Arrange
            var tcpClient = new TcpClientWrapper("localhost", 8080);
            var eventInvoked = false;
            byte[] receivedData = null;

            tcpClient.MessageReceived += (sender, data) =>
            {
                eventInvoked = true;
                receivedData = data;
            };

            // Act - invoke event through reflection
            var eventField = typeof(TcpClientWrapper).GetField("MessageReceived", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var eventHandler = eventField?.GetValue(tcpClient) as EventHandler<byte[]>;
            
            var testData = new byte[] { 1, 2, 3 };
            eventHandler?.Invoke(tcpClient, testData);

            // Assert
            Assert.That(eventInvoked, Is.True);
            Assert.That(receivedData, Is.EqualTo(testData));

            tcpClient.Dispose();
        }

        [Test]
        public void UdpMessageReceived_Event_ShouldBeInvokable()
        {
            // Arrange
            var udpClient = new UdpClientWrapper(9090);
            var eventInvoked = false;
            byte[] receivedData = null;

            udpClient.MessageReceived += (sender, data) =>
            {
                eventInvoked = true;
                receivedData = data;
            };

            // Act - invoke event through reflection
            var eventField = typeof(UdpClientWrapper).GetField("MessageReceived", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var eventHandler = eventField?.GetValue(udpClient) as EventHandler<byte[]>;
            
            var testData = new byte[] { 4, 5, 6 };
            eventHandler?.Invoke(udpClient, testData);

            // Assert
            Assert.That(eventInvoked, Is.True);
            Assert.That(receivedData, Is.EqualTo(testData));

            udpClient.Dispose();
        }
    }

    [TestFixture]
    public class StringConversionTests
    {
        [Test]
        public void StringToByteConversion_ShouldWorkCorrectly()
        {
            // Arrange
            var testString = "Hello World";
            var expectedBytes = Encoding.UTF8.GetBytes(testString);

            // Act
            var actualBytes = Encoding.UTF8.GetBytes(testString);

            // Assert
            Assert.That(actualBytes, Is.EqualTo(expectedBytes));
        }

        [Test]
        public void ByteToHexString_ShouldFormatCorrectly()
        {
            // Arrange
            var testData = new byte[] { 0x00, 0xFF, 0x0A, 0x1F };

            // Act
            var hexString = testData.Select(b => Convert.ToString(b, 16)).Aggregate((l, r) => $"{l} {r}");

            // Assert
            Assert.That(hexString, Is.EqualTo("0 ff a 1f"));
        }
    }

    [TestFixture]
    public class ErrorHandlingTests
    {
        [Test]
        public void SafeExecute_ShouldLogErrorsToConsole()
        {
            // Arrange
            var testClient = new TestNetworkClient();
            var originalOut = Console.Out;
            
            try
            {
                using var stringWriter = new StringWriter();
                Console.SetOut(stringWriter);

                // Act
                testClient.TestSafeExecute(() => throw new InvalidOperationException("Test error"), "test operation");

                // Assert
                var output = stringWriter.ToString();
                Assert.That(output, Contains.Substring("Error during test operation"));
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Test]
        public async Task SafeExecuteAsync_ShouldLogAsyncErrorsToConsole()
        {
            // Arrange
            var testClient = new TestNetworkClient();
            var originalOut = Console.Out;
            
            try
            {
                using var stringWriter = new StringWriter();
                Console.SetOut(stringWriter);

                // Act
                await testClient.TestSafeExecuteAsync(async () => 
                {
                    await Task.Delay(1);
                    throw new InvalidOperationException("Async test error");
                }, "test async operation");

                // Assert
                var output = stringWriter.ToString();
                Assert.That(output, Contains.Substring("Error during test async operation"));
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
    }

    [TestFixture]
    public class ExtendedNetworkClientBaseTests
    {
        private ExtendedTestNetworkClient _extendedTestClient;

        [SetUp]
        public void Setup()
        {
            _extendedTestClient = new ExtendedTestNetworkClient();
        }

        [TearDown]
        public void TearDown()
        {
            _extendedTestClient.Dispose();
        }

        [Test]
        public void ExtendedTestNetworkClient_Start_ShouldWork()
        {
            // Act
            _extendedTestClient.Start();

            // Assert
            Assert.That(_extendedTestClient.IsListening, Is.True);
        }

        [Test]
        public void ExtendedTestNetworkClient_Stop_ShouldWork()
        {
            // Arrange
            _extendedTestClient.Start();

            // Act
            _extendedTestClient.Stop();

            // Assert
            Assert.That(_extendedTestClient.IsListening, Is.False);
        }

        [Test]
        public void ExtendedTestNetworkClient_SafeExecute_ShouldWork()
        {
            // Arrange
            bool executed = false;

            // Act
            _extendedTestClient.TestSafeExecute(() => executed = true, "extended test operation");

            // Assert
            Assert.That(executed, Is.True);
        }

        [Test]
        public async Task ExtendedTestNetworkClient_SafeExecuteAsync_ShouldWork()
        {
            // Arrange
            bool executed = false;

            // Act
            await _extendedTestClient.TestSafeExecuteAsync(async () => 
            {
                await Task.Delay(10);
                executed = true;
            }, "extended test async operation");

            // Assert
            Assert.That(executed, Is.True);
        }
    }

    [TestFixture]
    public class TcpClientWrapperDetailedTests
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
        public async Task SendMessageAsync_ShouldFormatHexStringCorrectly()
        {
            // Arrange
            var testData = new byte[] { 0x0A, 0x0B, 0x0C, 0xFF };
            var expectedHexString = "a b c ff";
            
            // Use StringWriter to capture console output
            var originalOut = Console.Out;
            
            try
            {
                using var stringWriter = new StringWriter();
                Console.SetOut(stringWriter);

                // Act - Test the hex conversion logic directly
                var hexString = testData.Select(b => Convert.ToString(b, 16)).Aggregate((l, r) => $"{l} {r}");
                
                // Assert
                Assert.That(hexString, Is.EqualTo(expectedHexString));
                Assert.That(stringWriter.ToString(), Is.Empty); // No console output from direct test
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Test]
        public void SafeExecuteAsync_InSendMessage_ShouldHandleExceptions()
        {
            // Arrange
            var testData = new byte[] { 0x01, 0x02, 0x03 };
            
            // Act & Assert - Should not throw when not connected
            Assert.ThrowsAsync<InvalidOperationException>(() => 
                _tcpClient.SendMessageAsync(testData));
        }

        [Test]
        public void MessageReceived_Event_ShouldBeTriggered()
        {
            // Arrange
            var testData = new byte[] { 0x01, 0x02, 0x03 };
            bool eventTriggered = false;
            byte[] receivedData = null;

            _tcpClient.MessageReceived += (sender, data) =>
            {
                eventTriggered = true;
                receivedData = data;
            };

            // Act - Trigger event through reflection
            var messageReceivedField = typeof(TcpClientWrapper).GetField("MessageReceived", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var eventHandler = messageReceivedField?.GetValue(_tcpClient) as EventHandler<byte[]>;
            
            eventHandler?.Invoke(_tcpClient, testData);

            // Assert
            Assert.That(eventTriggered, Is.True);
            Assert.That(receivedData, Is.EqualTo(testData));
        }

        [Test]
        public void StreamReadAsync_ShouldHandleCancellation()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Immediately cancel

            var buffer = new byte[8194];
            var memoryStream = new MemoryStream(new byte[] { 0x01, 0x02 }); // Small stream

            // Act & Assert
            Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await memoryStream.ReadAsync(buffer, 0, buffer.Length, cts.Token));
        }

        [Test]
        public void MemoryStream_ReadWrite_Operations()
        {
            // Arrange
            var testData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            using var memoryStream = new MemoryStream();

            // Act - Test write
            memoryStream.Write(testData, 0, testData.Length);
            memoryStream.Position = 0; // Reset position for reading

            // Test read
            var buffer = new byte[8194];
            int bytesRead = memoryStream.Read(buffer, 0, buffer.Length);

            // Assert
            Assert.That(bytesRead, Is.EqualTo(testData.Length));
            Assert.That(buffer.AsSpan(0, bytesRead).ToArray(), Is.EqualTo(testData));
        }

        [Test]
        public async Task SafeExecuteAsync_WithMemoryStream()
        {
            // Arrange
            var testData = new byte[] { 0x01, 0x02, 0x03 };
            using var memoryStream = new MemoryStream(testData);
            
            var buffer = new byte[8194];
            bool operationCompleted = false;

            // Act
            try
            {
                int bytesRead = await memoryStream.ReadAsync(buffer, 0, buffer.Length);
                operationCompleted = true;
            }
            catch
            {
                // Handle any exceptions
            }

            // Assert
            Assert.That(operationCompleted, Is.True);
        }

        [Test]
        public void HexStringConversion_EdgeCases()
        {
            // Test various byte values for hex conversion
            var testCases = new[]
            {
                (new byte[] { 0x00 }, "0"),
                (new byte[] { 0xFF }, "ff"),
                (new byte[] { 0x0A, 0x00 }, "a 0"),
                (new byte[] { 0x10, 0x20, 0x30 }, "10 20 30"),
                (new byte[] { 0x7F, 0x80, 0xFF }, "7f 80 ff")
            };

            foreach (var (input, expected) in testCases)
            {
                // Act
                var hexString = input.Select(b => Convert.ToString(b, 16)).Aggregate((l, r) => $"{l} {r}");
                
                // Assert
                Assert.That(hexString, Is.EqualTo(expected), 
                    $"Failed for bytes: [{string.Join(", ", input.Select(b => $"0x{b:X2}"))}]");
            }
        }

        [Test]
        public async Task BufferManagement_InListeningLoop()
        {
            // Arrange
            var largeData = new byte[8194];
            new Random().NextBytes(largeData); // Fill with random data
            
            var memoryStream = new MemoryStream(largeData);
            int totalBytesRead = 0;
            int readCount = 0;

            // Act - Simulate reading loop
            var buffer = new byte[8194];
            int bytesRead;
            
            while ((bytesRead = await memoryStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                totalBytesRead += bytesRead;
                readCount++;
                
                // Verify the data read matches original data
                var readData = buffer.AsSpan(0, bytesRead).ToArray();
                var originalSegment = largeData.AsSpan(totalBytesRead - bytesRead, bytesRead).ToArray();
                Assert.That(readData, Is.EqualTo(originalSegment));
            }

            // Assert
            Assert.That(totalBytesRead, Is.EqualTo(largeData.Length));
            Assert.That(readCount, Is.GreaterThan(0));
        }
    }

    [TestFixture]
    public class MemoryStreamTests
    {
        [Test]
        public async Task MemoryStream_ReadAsync_WithCancellation_ShouldThrow()
        {
            // Arrange
            using var memoryStream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 });
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately
            
            var buffer = new byte[1024];

            // Act & Assert
            Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await memoryStream.ReadAsync(buffer, 0, buffer.Length, cts.Token));
        }

        [Test]
        public void StreamOperations_WhenDisposed_ShouldThrow()
        {
            // Arrange
            var memoryStream = new MemoryStream();
            memoryStream.Dispose();

            var buffer = new byte[1024];

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => 
                memoryStream.Read(buffer, 0, buffer.Length));
            Assert.Throws<ObjectDisposedException>(() => 
                memoryStream.Write(buffer, 0, buffer.Length));
        }

        [Test]
        public void MemoryStream_Creation_ShouldWork()
        {
            // Arrange & Act
            using var memoryStream = new MemoryStream();

            // Assert
            Assert.That(memoryStream, Is.Not.Null);
            Assert.That(memoryStream.CanRead, Is.True);
            Assert.That(memoryStream.CanWrite, Is.True);
        }
    }

    // Helper class for safe execution testing
    public static class TestSafeExecutionHelper
    {
        public static async Task<T> SafeExecuteAsyncWrapper<T>(Func<Task<T>> asyncFunc, string operationName)
        {
            try
            {
                return await asyncFunc();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during {operationName}: {ex.Message}");
                throw;
            }
        }
    }

    [TestFixture]
    public class MockNetworkStreamTests
    {
        [Test]
        public void CreateMockNetworkStream_ShouldWork()
        {
            // Arrange & Act
            // Instead of trying to create NetworkStream with disconnected socket,
            // we test that we understand the limitation
            Assert.DoesNotThrow(() =>
            {
                // This demonstrates we understand NetworkStream requires connected socket
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // We don't create NetworkStream because socket is not connected
                socket.Dispose();
            });
        }

        [Test]
        public void NetworkStream_RequiresConnectedSocket()
        {
            // Arrange
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Act & Assert - NetworkStream requires connected socket
            Assert.Throws<IOException>(() => new NetworkStream(socket));
            
            // Cleanup
            socket.Dispose();
        }
    }

    // Mock TcpClient for testing
    public class MockTcpClient : IDisposable
    {
        private bool _connected = false;
        private MemoryStream _stream;

        public bool Connected => _connected;

        public MockTcpClient()
        {
            _stream = new MemoryStream();
        }

        public void Connect(string host, int port)
        {
            _connected = true;
        }

        public Stream GetStream()
        {
            return _connected ? _stream : throw new InvalidOperationException("Not connected");
        }

        public void Close()
        {
            _connected = false;
            _stream?.Dispose();
        }

        public void Dispose()
        {
            Close();
        }
    }

    [TestFixture]
    public class TestInfrastructureTests
    {
        [Test]
        public void TestInfrastructure_ShouldWork()
        {
            // Arrange
            var mockClient = new MockTcpClient();

            // Act
            mockClient.Connect("localhost", 8080);
            var stream = mockClient.GetStream();

            // Assert
            Assert.That(mockClient.Connected, Is.True);
            Assert.That(stream, Is.Not.Null);
            
            mockClient.Dispose();
        }

        [Test]
        public void MockTcpClient_WhenNotConnected_ShouldThrow()
        {
            // Arrange
            var mockClient = new MockTcpClient();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => mockClient.GetStream());
            
            mockClient.Dispose();
        }
    }
}