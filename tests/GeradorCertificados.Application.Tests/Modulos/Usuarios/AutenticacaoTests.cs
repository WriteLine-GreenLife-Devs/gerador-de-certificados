using GeradorCertificados.Application.Usuarios;
using GeradorCertificados.Domain.Usuarios;
using Moq;

namespace GeradorCertificados.Application.Tests.Modulos.Usuarios;

[TestClass]
public sealed class AutenticacaoTests
{
    [TestMethod]
    public void NormalizarEmail_ComEspacosEMaiusculas_NormalizaValor()
    {
        var email = Usuario.NormalizarEmail("  ALUNO@EXEMPLO.COM  ");
        Assert.AreEqual("aluno@exemplo.com", email);
    }

    [TestMethod]
    public void NormalizarEmail_ComFormatoInvalido_LancaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Usuario.NormalizarEmail("email-invalido"));
    }

    [TestMethod]
    [DataRow("Curta1!")]
    [DataRow("SenhaSemEspecial1")]
    [DataRow("Senha@SemNumero")]
    public async Task CadastrarAsync_ComSenhaInvalida_LancaArgumentException(string senha)
    {
        var service = CriarService();
        await Assert.ThrowsAsync<ArgumentException>(() => service.CadastrarAsync("aluno@exemplo.com", senha, CancellationToken.None));
    }

    [TestMethod]
    public async Task CadastrarAsync_ComEmailJaCadastrado_LancaConflitoDeNegocioException()
    {
        var service = CriarService(existeEmail: true);
        await Assert.ThrowsAsync<ConflitoDeNegocioException>(() => service.CadastrarAsync("aluno@exemplo.com", "Senha@123", CancellationToken.None));
    }

    [TestMethod]
    public async Task CadastrarAsync_ComEmailJaCadastradoNoMock_NaoPersisteUsuarioELancaConflito()
    {
        var repositorioMock = new Mock<IUsuarioRepository>();
        repositorioMock
            .Setup(x => x.ExisteEmailAsync("aluno@exemplo.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = new AutenticacaoService(repositorioMock.Object, new SenhaServiceFake(), new TokenServiceFake());

        await Assert.ThrowsExactlyAsync<ConflitoDeNegocioException>(() => service.CadastrarAsync("aluno@exemplo.com", "Senha@123", CancellationToken.None));

        repositorioMock.Verify(x => x.ExisteEmailAsync("aluno@exemplo.com", It.IsAny<CancellationToken>()), Times.Once);
        repositorioMock.Verify(x => x.AdicionarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static AutenticacaoService CriarService(bool existeEmail = false) =>
        new(new UsuarioRepositoryFake(existeEmail), new SenhaServiceFake(), new TokenServiceFake());

    private sealed class UsuarioRepositoryFake(bool existeEmail) : IUsuarioRepository
    {
        public Task<bool> ExisteEmailAsync(string email, CancellationToken ct) => Task.FromResult(existeEmail);
        public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct) => Task.FromResult<Usuario?>(null);
        public Task AdicionarAsync(Usuario usuario, CancellationToken ct) => Task.CompletedTask;
    }
    private sealed class SenhaServiceFake : ISenhaService { public string GerarHash(Usuario usuario, string senha) => "hash"; public bool Verificar(Usuario usuario, string senha) => false; }
    private sealed class TokenServiceFake : ITokenService { public string Gerar(Usuario usuario) => "token"; }
}
