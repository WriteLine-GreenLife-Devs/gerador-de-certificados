using GeradorCertificados.Infrastructure.RabbitMq;

namespace GeradorCertificados.Application.Tests.Modulos.Certificados;

[TestClass]
public sealed class RabbitMqOptionsTests
{
    [TestMethod]
    public void Criar_ComDefaultsLocais_UsaConfiguracaoRabbitMqLocal()
    {
        var options = new RabbitMqOptions();

        Assert.AreEqual("localhost", options.Host);
        Assert.AreEqual("guest", options.Username);
        Assert.AreEqual("guest", options.Password);
        Assert.AreEqual("/", options.VirtualHost);
        Assert.AreEqual((ushort)5672, options.Port);
        Assert.IsFalse(options.UseSsl);
    }

    [TestMethod]
    public void Configurar_ComDadosCloudAmqp_PermiteHostVirtualHostPortaETls()
    {
        var options = new RabbitMqOptions
        {
            Host = "broker.example",
            Username = "cloud-user",
            Password = "cloud-password",
            VirtualHost = "cloud-vhost",
            Port = 5671,
            UseSsl = true
        };

        Assert.AreEqual("broker.example", options.Host);
        Assert.AreEqual("cloud-user", options.Username);
        Assert.AreEqual("cloud-password", options.Password);
        Assert.AreEqual("cloud-vhost", options.VirtualHost);
        Assert.AreEqual((ushort)5671, options.Port);
        Assert.IsTrue(options.UseSsl);
    }
}