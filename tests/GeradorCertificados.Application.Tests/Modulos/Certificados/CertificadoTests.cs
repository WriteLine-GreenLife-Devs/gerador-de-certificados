using GeradorCertificados.Domain.Certificados;

namespace GeradorCertificados.Application.Tests.Modulos.Certificados;

[TestClass]
public sealed class CertificadoTests
{
    [TestMethod]
    public void Criar_ComDadosValidos_CriaCertificado()
    {
        var solicitacaoId = Guid.NewGuid();

        var certificado = Certificado.Criar(solicitacaoId, "  Maria Silva  ");

        Assert.AreEqual(solicitacaoId, certificado.SolicitacaoId);
        Assert.AreEqual("Maria Silva", certificado.NomeAluno);
        Assert.AreEqual(StatusCertificado.Pendente, certificado.Status);
        Assert.IsNull(certificado.CaminhoArquivo);
        Assert.IsNull(certificado.DataGeracao);
    }

    [TestMethod]
    public void Criar_ComSolicitacaoIdVazia_LancaArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Certificado.Criar(Guid.Empty, "Maria"));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    public void Criar_ComNomeAlunoVazioOuWhitespace_LancaArgumentException(string nome)
    {
        Assert.ThrowsExactly<ArgumentException>(() => Certificado.Criar(Guid.NewGuid(), nome));
    }

    [TestMethod]
    public void Criar_ComNomeAlunoCom200Caracteres_Permite()
    {
        var nome = new string('A', 200);

        var certificado = Certificado.Criar(Guid.NewGuid(), nome);

        Assert.AreEqual(200, certificado.NomeAluno.Length);
    }

    [TestMethod]
    public void Criar_ComNomeAlunoCom201Caracteres_LancaArgumentException()
    {
        var nome = new string('A', 201);

        Assert.ThrowsExactly<ArgumentException>(() => Certificado.Criar(Guid.NewGuid(), nome));
    }

    [TestMethod]
    public void MarcarComoGerado_ComCaminhoValido_AtualizaEstado()
    {
        var certificado = Certificado.Criar(Guid.NewGuid(), "Maria Silva");

        certificado.MarcarComoGerado("/certificados/123.pdf");

        Assert.AreEqual(StatusCertificado.Gerado, certificado.Status);
        Assert.AreEqual("/certificados/123.pdf", certificado.CaminhoArquivo);
        Assert.IsNotNull(certificado.DataGeracao);
    }

    [TestMethod]
    public void MarcarComoGerado_SemCaminho_LancaArgumentException()
    {
        var certificado = Certificado.Criar(Guid.NewGuid(), "Maria Silva");

        Assert.ThrowsExactly<ArgumentException>(() => certificado.MarcarComoGerado(" "));
    }

    [TestMethod]
    public void MarcarComoFalha_ComEstadoPendente_AtualizaEstado()
    {
        var certificado = Certificado.Criar(Guid.NewGuid(), "Maria Silva");

        certificado.MarcarComoFalha();

        Assert.AreEqual(StatusCertificado.Falha, certificado.Status);
        Assert.IsNull(certificado.CaminhoArquivo);
        Assert.IsNull(certificado.DataGeracao);
    }

    [TestMethod]
    public void MarcarComoGerado_QuandoJaFinalizado_LancaInvalidOperationException()
    {
        var certificado = Certificado.Criar(Guid.NewGuid(), "Maria Silva");
        certificado.MarcarComoGerado("/certificados/123.pdf");

        Assert.ThrowsExactly<InvalidOperationException>(() => certificado.MarcarComoGerado("/certificados/456.pdf"));
    }

    [TestMethod]
    public void MarcarComoFalha_QuandoJaFinalizado_LancaInvalidOperationException()
    {
        var certificado = Certificado.Criar(Guid.NewGuid(), "Maria Silva");
        certificado.MarcarComoGerado("/certificados/123.pdf");

        Assert.ThrowsExactly<InvalidOperationException>(() => certificado.MarcarComoFalha());
    }
}
