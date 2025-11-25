using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace EchoServer.Tests
{
    // Інтерфейси для тестування
    public interface ITcpListener
    {
        Task<ITcpClient> AcceptTcpClientAsync();
        void Start();
        void Stop();
    }

    public interface ITcpClient : IDisposable
    {
        INetworkStream GetStream();
        void Close();
    }

    public interface INetworkStream : IDisposable
    {
        Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);
        Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default);
    }

    public interface ILogger
    {
        void Log(string message);
    }

    // Mock-класи для тестування
    public class TestNetworkStream : INetworkStream
    {
        private readonly Queue<byte[]> _dataToRead;
        private readonly List<byte[]> _writtenData;
        private bool _throwExceptionOnRead;
        private Exception _exceptionToThrow;

        public IReadOnlyList<byte[]> WrittenData => _writtenData;
        public bool IsDisposed { get; private set; }

        public TestNetworkStream()
        {
            _dataToRead = new Queue<byte[]>();
            _writtenData = new List<byte[]>();
        }

        public void SetDataToRead(byte[] data)
        {
            _dataToRead.Enqueue(data);
        }

        public void SetExceptionOnRead(Exception ex)
        {
            _throwExceptionOnRead = true;
            _exceptionToThrow = ex;
        }

        public Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_throwExceptionOnRead)
                throw _exceptionToThrow;

            if (_dataToRead.Count == 0)
                return Task.FromResult(0);

            var data = _dataToRead.Dequeue();
            data.CopyTo(buffer);
            return Task.FromResult(data.Length);
        }

        public Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            _writtenData.Add(buffer.ToArray());
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    public class TestTcpClient : ITcpClient
    {
        private readonly INetworkStream _stream;
        public bool IsClosed { get; private set; }
        public bool IsDisposed { get; private set; }

        public TestTcpClient(INetworkStream stream)
        {
            _stream = stream;
        }

        public INetworkStream GetStream() => _stream;
        
        public void Close()
        {
            IsClosed = true;
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    public class TestTcpListener : ITcpListener
    {
        private readonly Queue<ITcpClient> _clientsToAccept;
        private bool _isStarted;
        private bool _isStopped;

        public bool IsStarted => _isStarted;
        public bool IsStopped => _isStopped;

        public TestTcpListener()
        {
            _clientsToAccept = new Queue<ITcpClient>();
        }

        public void AddClient(ITcpClient client)
        {
            _clientsToAccept.Enqueue(client);
        }

        public Task<ITcpClient> AcceptTcpClientAsync()
        {
            if (_clientsToAccept.Count > 0)
                return Task.FromResult(_clientsToAccept.Dequeue());
            
            return Task.FromResult<ITcpClient>(null);
        }

        public void Start()
        {
            _isStarted = true;
        }

        public void Stop()
        {
            _isStopped = true;
        }
    }

    public class TestLogger : ILogger
    {
        public List<string> LogMessages { get; } = new List<string>();

        public void Log(string message)
        {
            LogMessages.Add(message);
        }
    }

    // Тестовий клас EchoServer для тестування
    public class TestableEchoServer
    {
        private ITcpListener _listener;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ILogger _logger;

        public TestableEchoServer(ITcpListener listener, ILogger logger = null)
        {
            _listener = listener;
            _cancellationTokenSource = new CancellationTokenSource();
            _logger = logger ?? new ConsoleLogger();
        }

        public async Task HandleClientAsync(ITcpClient client, CancellationToken token)
        {
            using (INetworkStream stream = client.GetStream())
            {
                try
                {
                    byte[] buffer = new byte[8192];

                    while (!token.IsCancellationRequested)
                    {
                        int bytesRead = await stream.ReadAsync(buffer.AsMemory(), token);
                        if (bytesRead == 0)
                            break;
                        await stream.WriteAsync(buffer.AsMemory(0, bytesRead), token);

                        _logger.Log($"Echoed {bytesRead} bytes to the client.");
                    }
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    _logger.Log($"Error: {ex.Message}");
                }
                finally
                {
                    client.Close();
                    _logger.Log("Client disconnected.");
                }
            }
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _listener?.Stop();
            _cancellationTokenSource.Dispose();
            _logger.Log("Server stopped.");
        }
    }

    public class ConsoleLogger : ILogger
    {
        public void Log(string message) => Console.WriteLine(message);
    }

    // Основний клас тестів
    public class EchoServerTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("Starting EchoServer tests...");
            
            TestEchoData();
            TestEmptyDataClosesConnection();
            TestExceptionLogging();
            TestServerStop();
            
            Console.WriteLine("All tests completed!");
        }

        public static void TestEchoData()
        {
            Console.WriteLine("Running TestEchoData...");
            
            try
            {
                // Arrange
                var testStream = new TestNetworkStream();
                var testData = new byte[] { 1, 2, 3, 4, 5 };
                testStream.SetDataToRead(testData);
                
                var testClient = new TestTcpClient(testStream);
                var testLogger = new TestLogger();
                var server = new TestableEchoServer(new TestTcpListener(), testLogger);

                // Act
                server.HandleClientAsync(testClient, CancellationToken.None).Wait();

                // Assert
                if (testStream.WrittenData.Count == 0)
                {
                    Console.WriteLine("FAIL: No data was written back");
                    return;
                }

                var echoedData = testStream.WrittenData[0];
                if (!testData.SequenceEqual(echoedData))
                {
                    Console.WriteLine("FAIL: Echoed data doesn't match original");
                    return;
                }

                if (!testLogger.LogMessages.Any(m => m.Contains($"Echoed {testData.Length} bytes")))
                {
                    Console.WriteLine("FAIL: No log message about echoed bytes");
                    return;
                }

                if (!testClient.IsClosed)
                {
                    Console.WriteLine("FAIL: Client was not closed");
                    return;
                }

                Console.WriteLine("PASS: TestEchoData");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL: TestEchoData - {ex.Message}");
            }
        }

        public static void TestEmptyDataClosesConnection()
        {
            Console.WriteLine("Running TestEmptyDataClosesConnection...");
            
            try
            {
                // Arrange
                var testStream = new TestNetworkStream();
                // No data set - simulates empty read (connection closed)
                var testClient = new TestTcpClient(testStream);
                var testLogger = new TestLogger();
                var server = new TestableEchoServer(new TestTcpListener(), testLogger);

                // Act
                server.HandleClientAsync(testClient, CancellationToken.None).Wait();

                // Assert
                if (testStream.WrittenData.Count > 0)
                {
                    Console.WriteLine("FAIL: Data was written when connection should be closed");
                    return;
                }

                if (!testClient.IsClosed)
                {
                    Console.WriteLine("FAIL: Client was not closed");
                    return;
                }

                if (!testLogger.LogMessages.Any(m => m.Contains("Client disconnected")))
                {
                    Console.WriteLine("FAIL: No disconnect log message");
                    return;
                }

                Console.WriteLine("PASS: TestEmptyDataClosesConnection");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL: TestEmptyDataClosesConnection - {ex.Message}");
            }
        }

        public static void TestExceptionLogging()
        {
            Console.WriteLine("Running TestExceptionLogging...");
            
            try
            {
                // Arrange
                var testStream = new TestNetworkStream();
                testStream.SetExceptionOnRead(new Exception("Test exception"));
                
                var testClient = new TestTcpClient(testStream);
                var testLogger = new TestLogger();
                var server = new TestableEchoServer(new TestTcpListener(), testLogger);

                // Act
                server.HandleClientAsync(testClient, CancellationToken.None).Wait();

                // Assert
                if (!testLogger.LogMessages.Any(m => m.Contains("Error: Test exception")))
                {
                    Console.WriteLine("FAIL: Error was not logged");
                    return;
                }

                if (!testClient.IsClosed)
                {
                    Console.WriteLine("FAIL: Client was not closed after exception");
                    return;
                }

                Console.WriteLine("PASS: TestExceptionLogging");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL: TestExceptionLogging - {ex.Message}");
            }
        }

        public static void TestServerStop()
        {
            Console.WriteLine("Running TestServerStop...");
            
            try
            {
                // Arrange
                var testListener = new TestTcpListener();
                var testLogger = new TestLogger();
                var server = new TestableEchoServer(testListener, testLogger);

                // Act
                server.Stop();

                // Assert
                if (!testListener.IsStopped)
                {
                    Console.WriteLine("FAIL: Listener was not stopped");
                    return;
                }

                if (!testLogger.LogMessages.Any(m => m.Contains("Server stopped")))
                {
                    Console.WriteLine("FAIL: No stop log message");
                    return;
                }

                Console.WriteLine("PASS: TestServerStop");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL: TestServerStop - {ex.Message}");
            }
        }
    }
}