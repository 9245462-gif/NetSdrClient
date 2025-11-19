using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;
using System.Reflection;

namespace NetSdrClientAppTests;

public class NetSdrClientTests
{
    NetSdrClient _client;
    Mock<ITcpClient> _tcpMock;
    Mock<IUdpClient> _updMock;

    public NetSdrClientTests() { }

    [SetUp]
    public void Setup()
    {
        _tcpMock = new Mock<ITcpClient>();
        _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
        });

        _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
        });

        _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>())).Callback<byte[]>((bytes) =>
        {
            _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, bytes);
        });

        _updMock = new Mock<IUdpClient>();

        _client = new NetSdrClient(_tcpMock.Object, _updMock.Object);
    }

    [Test]
    public async Task ConnectAsyncTest()
    {
        //act
        await _client.ConnectAsync();

        //assert
        _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
    }

    [Test]
    public async Task DisconnectWithNoConnectionTest()
    {
        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task DisconnectTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task StartIQNoConnectionTest()
    {

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        _tcpMock.VerifyGet(tcp => tcp.Connected, Times.AtLeastOnce);
    }

    [Test]
    public async Task StartIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
        Assert.That(_client.IQStarted, Is.True);
    }

    [Test]
    public async Task StopIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StopIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(tcp => tcp.StopListening(), Times.Once);
        Assert.That(_client.IQStarted, Is.False);
    }

    [Test]
    public async Task ChangeFrequencyAsync_ShouldSendCorrectMessage()
    {
        // Arrange
        await _client.ConnectAsync(); // встановлює Connected=true
        long freq = 20_000_000;
        int channel = 1;

        byte[] sentMsg = null;
        _tcpMock.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>()))
            .Callback<byte[]>(msg => sentMsg = msg)
            .Returns(Task.CompletedTask);

        // Act
        await _client.ChangeFrequencyAsync(freq, channel);

        // Assert
        _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.AtLeastOnce);
        Assert.NotNull(sentMsg);
        Assert.That(sentMsg.Length, Is.GreaterThan(5));
        
        // перший байт тіла має бути channel
        Assert.That(sentMsg[5], Is.EqualTo((byte)channel));
    }

    [Test]
    public async Task SendTcpRequest_ShouldReturnResponseFromEvent()
    {
        // Arrange
        await _client.ConnectAsync();

        byte[] expectedResponse = { 0x10, 0x20, 0x30 };

        _tcpMock.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>()))
            .Callback(() =>
            {
                _tcpMock.Raise(t => t.MessageReceived += null, _tcpMock.Object, expectedResponse);
            })
            .Returns(Task.CompletedTask);

        // Act — виклик приватного методу через reflection
        var response = await CallSendTcpRequest(_client, new byte[] { 1, 2, 3 });

        // Assert
        Assert.NotNull(response);
        Assert.That(response, Is.EqualTo(expectedResponse));
    }

    private Task<byte[]> CallSendTcpRequest(NetSdrClient client, byte[] data)
    {
        var method = typeof(NetSdrClient)
            .GetMethod("SendTcpRequest", BindingFlags.NonPublic | BindingFlags.Instance);
        return (Task<byte[]>)method.Invoke(client, new object[] { data });
    }

    [Test]
    public void TcpMessageReceived_WithoutPendingRequest_DoesNotThrow()
    {
        // Arrange
        byte[] raw = { 1, 2, 3 };

        // Act & Assert
        Assert.DoesNotThrow(() =>
            _tcpMock.Raise(t => t.MessageReceived += null, _tcpMock.Object, raw)
        );
    }

    [Test]
    public void Disconnect_ShouldAlwaysCallUnderlyingTcp()
    {
        // Act
        _client.Disconect();

        // Assert
        _tcpMock.Verify(t => t.Disconnect(), Times.Once);
    }

    [Test]
    public async Task StartIQAsync_NoConnection_DoesNothing()
    {
        // Arrange — tcp.Connected = false (за замовчуванням)

        // Act
        await _client.StartIQAsync();

        // Assert
        _updMock.Verify(u => u.StartListeningAsync(), Times.Never);
        Assert.False(_client.IQStarted);
    }
}
