namespace GeradorCertificados.Api.Contracts.Auth;
public sealed record CadastroRequest(string Email, string Senha);
public sealed record LoginRequest(string Email, string Senha);
public sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiraEm);
