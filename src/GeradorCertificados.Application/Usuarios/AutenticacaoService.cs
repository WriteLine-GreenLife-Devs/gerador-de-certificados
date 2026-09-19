using GeradorCertificados.Domain.Usuarios;

namespace GeradorCertificados.Application.Usuarios;

public sealed class AutenticacaoService(IUsuarioRepository usuarios, ISenhaService senhas, ITokenService tokens)
{
    public async Task CadastrarAsync(string email, string senha, CancellationToken ct)
    {
        ValidarSenha(senha);
        var normalizado = Usuario.NormalizarEmail(email);
        if (await usuarios.ExisteEmailAsync(normalizado, ct)) throw new ConflitoDeNegocioException("Já existe um usuário com este email.");
        var usuario = Usuario.Criar(normalizado, "pendente");
        var comHash = Usuario.Criar(normalizado, senhas.GerarHash(usuario, senha));
        await usuarios.AdicionarAsync(comHash, ct);
    }
    public async Task<TokenDeAcesso> LoginAsync(string email, string senha, CancellationToken ct)
    {
        var usuario = await usuarios.ObterPorEmailAsync(Usuario.NormalizarEmail(email), ct);
        if (usuario is null || !senhas.Verificar(usuario, senha)) throw new CredenciaisInvalidasException();
        return new TokenDeAcesso(tokens.Gerar(usuario), DateTimeOffset.UtcNow.AddHours(1));
    }
    private static void ValidarSenha(string senha)
    {
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < 8 || !senha.Any(char.IsDigit) || !senha.Any(c => !char.IsLetterOrDigit(c)))
            throw new ArgumentException("A senha deve ter ao menos 8 caracteres, um dígito e um caractere não alfanumérico.", nameof(senha));
    }
}
