using NetArchTest.Rules;
using NUnit.Framework;
using System.Linq;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests
{
    public class ArchitectureTests
    {
        [Test]
        public void App_Should_Not_Depend_On_EchoServer()
        {
            var result = Types.InAssembly(typeof(NetSdrClientApp.NetSdrClient).Assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp")
                .ShouldNot()
                .HaveDependencyOn("EchoServer")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True, 
                GetFailingTypesMessage(result));
        }

        [Test]
        public void Messages_Should_Not_Depend_On_Networking()
        {
            var result = Types.InAssembly(typeof(NetSdrClientApp.Messages.NetSdrMessageHelper).Assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp.Messages")
                .ShouldNot()
                .HaveDependencyOn("NetSdrClientApp.Networking")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True, 
                GetFailingTypesMessage(result));
        }

        [Test]
        public void Networking_Should_Not_Depend_On_Messages()
        {
            var result = Types.InAssembly(typeof(ITcpClient).Assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp.Networking")
                .ShouldNot()
                .HaveDependencyOn("NetSdrClientApp.Messages")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True, 
                GetFailingTypesMessage(result));
        }

        [Test]
        public void Interfaces_Should_Not_Have_Dependencies_On_Concrete_Implementations()
        {
            var result = Types.InCurrentDomain()
                .That().AreInterfaces()
                .ShouldNot().HaveDependencyOnAny(
                    "NetSdrClientApp.Networking", 
                    "NetSdrClientApp.Messages")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True, 
                GetFailingTypesMessage(result));
        }

        [Test]
        public void Networking_Classes_Should_Implement_Correct_Interfaces()
        {
            // TcpClientWrapper повинен реалізовувати ITcpClient
            var tcpResult = Types.InCurrentDomain()
                .That().HaveName("TcpClientWrapper")
                .Should().ImplementInterface(typeof(ITcpClient))
                .GetResult();

            // UdpClientWrapper повинен реалізовувати IUdpClient
            var udpResult = Types.InCurrentDomain()
                .That().HaveName("UdpClientWrapper")
                .Should().ImplementInterface(typeof(IUdpClient))
                .GetResult();

            // NetworkClientBase може бути абстрактним і не реалізовувати інтерфейси напряму
            var baseResult = Types.InCurrentDomain()
                .That().HaveName("NetworkClientBase")
                .Should().BeAbstract()
                .GetResult();

            Assert.That(tcpResult.IsSuccessful, Is.True, "TcpClientWrapper should implement ITcpClient");
            Assert.That(udpResult.IsSuccessful, Is.True, "UdpClientWrapper should implement IUdpClient");
            Assert.That(baseResult.IsSuccessful, Is.True, "NetworkClientBase should be abstract");
        }

        [Test]
        public void Core_Should_Not_Depend_On_External_Libraries()
        {
            var result = Types.InCurrentDomain()
                .That().ResideInNamespace("NetSdrClientApp")
                .ShouldNot().HaveDependencyOnAny(
                    "Newtonsoft.Json",
                    "System.Windows.Forms")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True, 
                GetFailingTypesMessage(result));
        }

        [Test]
        public void All_Classes_Should_Be_Public()
        {
            var result = Types.InCurrentDomain()
                .That().AreClasses()
                .And().ResideInNamespace("NetSdrClientApp")
                .Should().BePublic()
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True, 
                GetFailingTypesMessage(result));
        }

        private string GetFailingTypesMessage(TestResult result)
        {
            if (result.FailingTypes?.Any() == true)
            {
                return $"Failing types: {string.Join(", ", result.FailingTypes.Select(t => t.FullName))}";
            }
            return "No failing types details available";
        }
    }
}