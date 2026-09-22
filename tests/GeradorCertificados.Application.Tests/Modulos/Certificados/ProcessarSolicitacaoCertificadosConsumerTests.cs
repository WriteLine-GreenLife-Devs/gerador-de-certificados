using GeradorCertificados.Application.Certificados;
using GeradorCertificados.Application.Certificados.Mensagens;
using GeradorCertificados.Domain.Certificados;
using GeradorCertificados.Infrastructure.Certificados;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GeradorCertificados.Application.Tests.Modulos.Certificados;

[TestClass]
public sealed class ProcessarSolicitacaoCertificadosConsumerTests
{
    [TestMethod]
    public async Task Consume_ComSolicitacaoPendente_AtualizaParaGerandoCertificados()
    {
        var solicitacao = SolicitacaoCertificado.Criar(Guid.NewGuid(), ["Maria"]);
        var repository = new FakeSolicitacaoCertificadoRepository(solicitacao);
        var consumer = new ProcessarSolicitacaoCertificadosConsumer(repository, NullLogger<ProcessarSolicitacaoCertificadosConsumer>.Instance);

        await consumer.ProcessarAsync(solicitacao.Id, CancellationToken.None);

        Assert.AreEqual(StatusSolicitacaoCertificado.GerandoCertificados, solicitacao.Status);
        Assert.HasCount(1, solicitacao.Certificados);
        Assert.HasCount(1, solicitacao.Certificados.Where(x => x.Status == StatusCertificado.Pendente));
    }

    [TestMethod]
    public async Task Consume_ComSolicitacaoGerandoCertificados_NaoReprocessa()
    {
        var solicitacao = SolicitacaoCertificado.Criar(Guid.NewGuid(), ["Maria"]);
        solicitacao.IniciarProcessamento();
        var repository = new FakeSolicitacaoCertificadoRepository(solicitacao);
        var consumer = new ProcessarSolicitacaoCertificadosConsumer(repository, NullLogger<ProcessarSolicitacaoCertificadosConsumer>.Instance);

        await consumer.ProcessarAsync(solicitacao.Id, CancellationToken.None);

        Assert.AreEqual(StatusSolicitacaoCertificado.GerandoCertificados, solicitacao.Status);
    }

    [TestMethod]
    public async Task Consume_ComSolicitacaoGerandoZip_Ignora()
    {
        var solicitacao = SolicitacaoCertificado.Criar(Guid.NewGuid(), ["Maria"]);
        solicitacao.IniciarProcessamento();
        solicitacao.IniciarGeracaoZip();
        var repository = new FakeSolicitacaoCertificadoRepository(solicitacao);
        var consumer = new ProcessarSolicitacaoCertificadosConsumer(repository, NullLogger<ProcessarSolicitacaoCertificadosConsumer>.Instance);

        await consumer.ProcessarAsync(solicitacao.Id, CancellationToken.None);

        Assert.AreEqual(StatusSolicitacaoCertificado.GerandoZip, solicitacao.Status);
    }

    [TestMethod]
    public async Task Consume_ComSolicitacaoConcluida_Ignora()
    {
        var solicitacao = SolicitacaoCertificado.Criar(Guid.NewGuid(), ["Maria"]);
        solicitacao.IniciarProcessamento();
        solicitacao.IniciarGeracaoZip();
        solicitacao.Concluir();
        var repository = new FakeSolicitacaoCertificadoRepository(solicitacao);
        var consumer = new ProcessarSolicitacaoCertificadosConsumer(repository, NullLogger<ProcessarSolicitacaoCertificadosConsumer>.Instance);

        await consumer.ProcessarAsync(solicitacao.Id, CancellationToken.None);

        Assert.AreEqual(StatusSolicitacaoCertificado.Concluido, solicitacao.Status);
    }

    [TestMethod]
    public async Task Consume_ComSolicitacaoFalha_Ignora()
    {
        var solicitacao = SolicitacaoCertificado.Criar(Guid.NewGuid(), ["Maria"]);
        solicitacao.MarcarFalha();
        var repository = new FakeSolicitacaoCertificadoRepository(solicitacao);
        var consumer = new ProcessarSolicitacaoCertificadosConsumer(repository, NullLogger<ProcessarSolicitacaoCertificadosConsumer>.Instance);

        await consumer.ProcessarAsync(solicitacao.Id, CancellationToken.None);

        Assert.AreEqual(StatusSolicitacaoCertificado.Falha, solicitacao.Status);
    }

    [TestMethod]
    public async Task Consume_ComSolicitacaoInexistente_TrataComSeguranca()
    {
        var repository = new FakeSolicitacaoCertificadoRepository(null);
        var consumer = new ProcessarSolicitacaoCertificadosConsumer(repository, NullLogger<ProcessarSolicitacaoCertificadosConsumer>.Instance);

        await consumer.ProcessarAsync(Guid.NewGuid(), CancellationToken.None);
    }

    [TestMethod]
    public async Task Consume_ComMensagem_PublicaSolicitacaoIdCorreto()
    {
        var solicitacao = SolicitacaoCertificado.Criar(Guid.NewGuid(), ["Maria"]);
        var publisher = new Mock<IPublishEndpoint>();

        var publicador = new MassTransitCertificadosMensagemPublisher(publisher.Object);
        await publicador.PublicarSolicitacaoAsync(solicitacao.Id, CancellationToken.None);

        publisher.Verify(x => x.Publish(
            It.Is<ProcessarSolicitacaoCertificados>(msg => msg.SolicitacaoId == solicitacao.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class FakeSolicitacaoCertificadoRepository(SolicitacaoCertificado? solicitacao) : ISolicitacaoCertificadoRepository
    {
        public Task AdicionarAsync(SolicitacaoCertificado solicitacao, CancellationToken ct) => Task.CompletedTask;

        public Task<SolicitacaoCertificado?> ObterPorIdAsync(Guid id, CancellationToken ct) => Task.FromResult(solicitacao);

        public Task<SolicitacaoCertificado?> ObterMaisRecentePorCursoAsync(Guid cursoId, CancellationToken ct) => Task.FromResult(solicitacao);

        public Task<IReadOnlyCollection<SolicitacaoCertificado>> ObterPorCursoAsync(Guid cursoId, CancellationToken ct)
            => Task.FromResult<IReadOnlyCollection<SolicitacaoCertificado>>(solicitacao is null ? Array.Empty<SolicitacaoCertificado>() : new[] { solicitacao });

        public Task<IReadOnlyCollection<Certificado>> ObterCertificadosPorSolicitacaoAsync(Guid solicitacaoId, CancellationToken ct)
            => Task.FromResult<IReadOnlyCollection<Certificado>>(solicitacao is null ? Array.Empty<Certificado>() : solicitacao.Certificados.ToList());

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
