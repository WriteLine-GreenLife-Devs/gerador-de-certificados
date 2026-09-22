using GeradorCertificados.Domain.Usuarios;

namespace GeradorCertificados.Application.Tests.Modulos.Usuarios;

[TestClass]
public sealed class UsuarioTests
{
    [TestMethod]
    public void Criar_ComDadosValidos_CriaUsuario()
    {
        var antes = DateTimeOffset.UtcNow;

        var usuario = Usuario.Criar("  ALUNO@EXEMPLO.COM  ", "hash-validado");

        var depois = DateTimeOffset.UtcNow;
        Assert.AreNotEqual(Guid.Empty, usuario.Id);
        Assert.AreEqual("aluno@exemplo.com", usuario.Email);
        Assert.AreEqual("hash-validado", usuario.SenhaHash);
        Assert.IsTrue(usuario.DataCadastro >= antes && usuario.DataCadastro <= depois);
    }

    [TestMethod]
    public void Criar_ComSenhaHashVazia_LancaArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Usuario.Criar("aluno@exemplo.com", " "));
    }
}