using GeradorCertificados.Domain.Certificados;

namespace GeradorCertificados.Application.Tests.Modulos.Certificados;

[TestClass]
public sealed class SolicitacaoCertificadoTests
{
    [TestMethod]
    public void Criar_ComDadosValidos_CriaSolicitacao()
    {
        var cursoId = Guid.NewGuid();

        var solicitacao = SolicitacaoCertificado.Criar(cursoId, ["Maria", "José"]);

        Assert.AreEqual(cursoId, solicitacao.CursoId);
        Assert.AreEqual(StatusSolicitacaoCertificado.Pendente, solicitacao.Status);
        Assert.HasCount(2, solicitacao.Certificados);
        Assert.HasCount(2, solicitacao.Certificados.Where(x => x.Status == StatusCertificado.Pendente));
    }

    [TestMethod]
    public void Criar_ComCursoIdVazio_LancaArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => SolicitacaoCertificado.Criar(Guid.Empty, ["Maria"]));
    }

    [TestMethod]
    public void Criar_ComListaVazia_LancaArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => SolicitacaoCertificado.Criar(Guid.NewGuid(), []));
    }

    [TestMethod]
    public void Criar_ComUmAluno_CriaUmCertificado()
    {
        var solicitacao = SolicitacaoCertificado.Criar(Guid.NewGuid(), ["Maria"]);

        Assert.HasCount(1, solicitacao.Certificados);
        Assert.AreEqual("Maria", solicitacao.Certificados.Single().NomeAluno);
    }

    [TestMethod]
    public void Criar_ComVariosAlunos_GeraUmCertificadoPorAluno()
    {
        var nomes = new[] { "Maria", "José", "Ana" };

        var solicitacao = SolicitacaoCertificado.Criar(Guid.NewGuid(), nomes);

        Assert.HasCount(nomes.Length, solicitacao.Certificados);
        CollectionAssert.AreEquivalent(nomes, solicitacao.Certificados.Select(x => x.NomeAluno).ToArray());
    }

    [TestMethod]
    public void Criar_ComNomeAlunoWhitespace_Trimado()
    {
        var solicitacao = SolicitacaoCertificado.Criar(Guid.NewGuid(), ["  Maria   "]);

        Assert.AreEqual("Maria", solicitacao.Certificados.Single().NomeAluno);
    }

    [TestMethod]
    public void IniciarProcessamento_QuandoPendente_AtualizaStatus()
    {
        var solicitacao = SolicitacaoCertificado.Criar(Guid.NewGuid(), ["Maria"]);

        solicitacao.IniciarProcessamento();

        Assert.AreEqual(StatusSolicitacaoCertificado.GerandoCertificados, solicitacao.Status);
    }

    [TestMethod]
    public void IniciarGeracaoZip_AposGerandoCertificados_AtualizaStatus()
    {
        var solicitacao = SolicitacaoCertificado.Criar(Guid.NewGuid(), ["Maria"]);
        solicitacao.IniciarProcessamento();

        solicitacao.IniciarGeracaoZip();

        Assert.AreEqual(StatusSolicitacaoCertificado.GerandoZip, solicitacao.Status);
    }

    [TestMethod]
    public void Concluir_AposGeracaoZip_AtualizaStatus()
    {
        var solicitacao = SolicitacaoCertificado.Criar(Guid.NewGuid(), ["Maria"]);
        solicitacao.IniciarProcessamento();
        solicitacao.IniciarGeracaoZip();

        solicitacao.Concluir();

        Assert.AreEqual(StatusSolicitacaoCertificado.Concluido, solicitacao.Status);
    }

    [TestMethod]
    public void IniciarProcessamento_QuandoJaEmEstadoFinal_LancaInvalidOperationException()
    {
        var solicitacao = SolicitacaoCertificado.Criar(Guid.NewGuid(), ["Maria"]);
        solicitacao.IniciarProcessamento();
        solicitacao.IniciarGeracaoZip();
        solicitacao.Concluir();

        Assert.ThrowsExactly<InvalidOperationException>(() => solicitacao.IniciarProcessamento());
    }

    [TestMethod]
    public void IniciarGeracaoZip_QuandoStatusInvalido_LancaInvalidOperationException()
    {
        var solicitacao = SolicitacaoCertificado.Criar(Guid.NewGuid(), ["Maria"]);

        Assert.ThrowsExactly<InvalidOperationException>(() => solicitacao.IniciarGeracaoZip());
    }

    [TestMethod]
    public void MarcarFalha_QuandoStatusPendente_AtualizaStatus()
    {
        var solicitacao = SolicitacaoCertificado.Criar(Guid.NewGuid(), ["Maria"]);

        solicitacao.MarcarFalha();

        Assert.AreEqual(StatusSolicitacaoCertificado.Falha, solicitacao.Status);
    }

    [TestMethod]
    public void MarcarFalha_QuandoStatusConcluido_LancaInvalidOperationException()
    {
        var solicitacao = SolicitacaoCertificado.Criar(Guid.NewGuid(), ["Maria"]);
        solicitacao.IniciarProcessamento();
        solicitacao.IniciarGeracaoZip();
        solicitacao.Concluir();

        Assert.ThrowsExactly<InvalidOperationException>(() => solicitacao.MarcarFalha());
    }
}
