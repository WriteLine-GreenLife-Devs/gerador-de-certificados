namespace GeradorCertificados.Infrastructure.RabbitMq;

public sealed class RabbitMqOptions
{
    public string Host { get; set; } = "localhost";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public ushort Port { get; set; } = 5672;
    public bool UseSsl { get; set; }
}
