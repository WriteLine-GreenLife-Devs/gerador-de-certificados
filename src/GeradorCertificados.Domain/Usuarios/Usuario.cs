using System.Net.Mail;

namespace GeradorCertificados.Domain.Usuarios;

public sealed class Usuario
{
    private Usuario() { Email = null!; SenhaHash = null!; }
    private Usuario(string email, string senhaHash) { Id = Guid.NewGuid(); Email = email; SenhaHash = senhaHash; DataCadastro = DateTimeOffset.UtcNow; }
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string SenhaHash { get; private set; }
    public DateTimeOffset DataCadastro { get; private set; }

    public static Usuario Criar(string email, string senhaHash)
    {
        var normalizado = NormalizarEmail(email);
        if (string.IsNullOrWhiteSpace(senhaHash)) throw new ArgumentException("A senha é obrigatória.", nameof(senhaHash));
        return new Usuario(normalizado, senhaHash);
    }

    public static string NormalizarEmail(string email)
    {
        var valor = email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(valor)) throw new ArgumentException("O email é obrigatório.", nameof(email));
        try { _ = new MailAddress(valor); } catch (FormatException) { throw new ArgumentException("O email é inválido.", nameof(email)); }
        return valor;
    }
}
