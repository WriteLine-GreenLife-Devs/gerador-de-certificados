using GeradorCertificados.Domain.Usuarios;

namespace GeradorCertificados.Application.Usuarios;

public interface IUsuarioRepository { Task<bool> ExisteEmailAsync(string email, CancellationToken ct); Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct); Task AdicionarAsync(Usuario usuario, CancellationToken ct); }
public interface ISenhaService { string GerarHash(Usuario usuario, string senha); bool Verificar(Usuario usuario, string senha); }
public interface ITokenService { string Gerar(Usuario usuario); }
public sealed record TokenDeAcesso(string AccessToken, DateTimeOffset ExpiraEm);
public sealed class ConflitoDeNegocioException(string message) : Exception(message);
public sealed class CredenciaisInvalidasException() : Exception("Email ou senha inválidos.");
