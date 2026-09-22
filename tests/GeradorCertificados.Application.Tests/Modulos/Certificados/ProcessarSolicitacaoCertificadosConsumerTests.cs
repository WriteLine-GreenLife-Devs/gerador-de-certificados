using GeradorCertificados.Application.Certificados;
using GeradorCertificados.Application.Certificados.Mensagens;
using GeradorCertificados.Application.Cursos;
using GeradorCertificados.Domain.Certificados;
using GeradorCertificados.Domain.Cursos;
using GeradorCertificados.Infrastructure.Certificados;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GeradorCertificados.Application.Tests.Modulos.Certificados;

[TestClass]
public sealed class ProcessarSolicitacaoCertificadosConsumerTests
{
    [TestMethod]
    public async Task Consume_ComSolicitacaoPendente_AtualizaParaGerandoCertificadosEProcessaCertificado()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var solicitacao = SolicitacaoCertificado.Criar(curso.Id, ["Maria"]);
        var repository = new FakeSolicitacaoCertificadoRepository(solicitacao);
        var cursoRepository = new FakeCursoRepository(curso);
        var storage = new FakeArmazenamentoCertificadoPdf();
        var generator = new FakeGeradorPdfCertificado();
        var consumer = new ProcessarSolicitacaoCertificadosConsumer(repository, cursoRepository, generator, storage, NullLogger<ProcessarSolicitacaoCertificadosConsumer>.Instance);

        await consumer.ProcessarAsync(solicitacao.Id, CancellationToken.None);

        Assert.AreEqual(StatusSolicitacaoCertificado.GerandoZip, solicitacao.Status);
        Assert.HasCount(1, solicitacao.Certificados);
        Assert.AreEqual(StatusCertificado.Gerado, solicitacao.Certificados.Single().Status);
        Assert.IsNotNull(solicitacao.Certificados.Single().DataGeracao);
        Assert.IsTrue(solicitacao.Certificados.Single().CaminhoArquivo!.Contains("certificados/", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(1, generator.QuantidadeChamadas);
    }

    [TestMethod]
    public async Task Consume_ComSolicitacaoGerandoCertificados_ContinuaProcessandoCertificadosPendentes()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var solicitacao = SolicitacaoCertificado.Criar(curso.Id, ["Maria", "José"]);
        solicitacao.IniciarProcessamento();
        var repository = new FakeSolicitacaoCertificadoRepository(solicitacao);
        var cursoRepository = new FakeCursoRepository(curso);
        var storage = new FakeArmazenamentoCertificadoPdf();
        var generator = new FakeGeradorPdfCertificado();
        var consumer = new ProcessarSolicitacaoCertificadosConsumer(repository, cursoRepository, generator, storage, NullLogger<ProcessarSolicitacaoCertificadosConsumer>.Instance);

        await consumer.ProcessarAsync(solicitacao.Id, CancellationToken.None);

        Assert.AreEqual(StatusSolicitacaoCertificado.GerandoZip, solicitacao.Status);
        Assert.AreEqual(2, solicitacao.Certificados.Count(x => x.Status == StatusCertificado.Gerado));
    }

    [TestMethod]
    public async Task Consume_ComCertificadoFalhando_ContinuaParaProximoAluno()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var solicitacao = SolicitacaoCertificado.Criar(curso.Id, ["Maria", "José", "Ana"]);
        var repository = new FakeSolicitacaoCertificadoRepository(solicitacao);
        var cursoRepository = new FakeCursoRepository(curso);
        var storage = new FakeArmazenamentoCertificadoPdf();
        var generator = new FakeGeradorPdfCertificado(gerarFalhaPara: "José");
        var consumer = new ProcessarSolicitacaoCertificadosConsumer(repository, cursoRepository, generator, storage, NullLogger<ProcessarSolicitacaoCertificadosConsumer>.Instance);

        await consumer.ProcessarAsync(solicitacao.Id, CancellationToken.None);

        Assert.AreEqual(StatusSolicitacaoCertificado.GerandoZip, solicitacao.Status);
        Assert.AreEqual(StatusCertificado.Gerado, solicitacao.Certificados.Single(x => x.NomeAluno == "Maria").Status);
        Assert.AreEqual(StatusCertificado.Falha, solicitacao.Certificados.Single(x => x.NomeAluno == "José").Status);
        Assert.AreEqual(StatusCertificado.Gerado, solicitacao.Certificados.Single(x => x.NomeAluno == "Ana").Status);
    }

    [TestMethod]
    public async Task Consume_ComSolicitacaoGerandoCertificados_NaoReprocessa()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var solicitacao = SolicitacaoCertificado.Criar(curso.Id, ["Maria"]);
        solicitacao.IniciarProcessamento();
        var repository = new FakeSolicitacaoCertificadoRepository(solicitacao);
        var consumer = new ProcessarSolicitacaoCertificadosConsumer(
            repository,
            new FakeCursoRepository(curso),
            new FakeGeradorPdfCertificado(),
            new FakeArmazenamentoCertificadoPdf(),
            NullLogger<ProcessarSolicitacaoCertificadosConsumer>.Instance);

        await consumer.ProcessarAsync(solicitacao.Id, CancellationToken.None);

        Assert.AreEqual(StatusSolicitacaoCertificado.GerandoZip, solicitacao.Status);
    }

    [TestMethod]
    public async Task Consume_ComPdfJaExistente_NaoDuplicaArquivoMasMarcaComoGerado()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var solicitacao = SolicitacaoCertificado.Criar(curso.Id, ["Maria"]);
        var repository = new FakeSolicitacaoCertificadoRepository(solicitacao);
        var storage = new FakeArmazenamentoCertificadoPdf(arquivoExiste: true);
        var generator = new FakeGeradorPdfCertificado();
        var consumer = new ProcessarSolicitacaoCertificadosConsumer(
            repository,
            new FakeCursoRepository(curso),
            generator,
            storage,
            NullLogger<ProcessarSolicitacaoCertificadosConsumer>.Instance);

        await consumer.ProcessarAsync(solicitacao.Id, CancellationToken.None);

        Assert.AreEqual(StatusSolicitacaoCertificado.GerandoZip, solicitacao.Status);
        Assert.AreEqual(StatusCertificado.Gerado, solicitacao.Certificados.Single().Status);
        Assert.IsNotNull(solicitacao.Certificados.Single().CaminhoArquivo);
        Assert.IsNotNull(solicitacao.Certificados.Single().DataGeracao);
        Assert.AreEqual(0, storage.QuantidadeSalvamentos);
        Assert.AreEqual(1, generator.QuantidadeChamadas);
    }

    [TestMethod]
    public async Task Consume_ComSolicitacaoGerandoZip_Ignora()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var solicitacao = SolicitacaoCertificado.Criar(curso.Id, ["Maria"]);
        solicitacao.IniciarProcessamento();
        solicitacao.IniciarGeracaoZip();
        var repository = new FakeSolicitacaoCertificadoRepository(solicitacao);
        var consumer = new ProcessarSolicitacaoCertificadosConsumer(
            repository,
            new FakeCursoRepository(curso),
            new FakeGeradorPdfCertificado(),
            new FakeArmazenamentoCertificadoPdf(),
            NullLogger<ProcessarSolicitacaoCertificadosConsumer>.Instance);

        await consumer.ProcessarAsync(solicitacao.Id, CancellationToken.None);

        Assert.AreEqual(StatusSolicitacaoCertificado.GerandoZip, solicitacao.Status);
    }

    [TestMethod]
    public async Task Consume_ComSolicitacaoConcluida_Ignora()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var solicitacao = SolicitacaoCertificado.Criar(curso.Id, ["Maria"]);
        solicitacao.IniciarProcessamento();
        solicitacao.IniciarGeracaoZip();
        solicitacao.Concluir();
        var repository = new FakeSolicitacaoCertificadoRepository(solicitacao);
        var consumer = new ProcessarSolicitacaoCertificadosConsumer(
            repository,
            new FakeCursoRepository(curso),
            new FakeGeradorPdfCertificado(),
            new FakeArmazenamentoCertificadoPdf(),
            NullLogger<ProcessarSolicitacaoCertificadosConsumer>.Instance);

        await consumer.ProcessarAsync(solicitacao.Id, CancellationToken.None);

        Assert.AreEqual(StatusSolicitacaoCertificado.Concluido, solicitacao.Status);
    }

    [TestMethod]
    public async Task Consume_ComSolicitacaoFalha_Ignora()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var solicitacao = SolicitacaoCertificado.Criar(curso.Id, ["Maria"]);
        solicitacao.MarcarFalha();
        var repository = new FakeSolicitacaoCertificadoRepository(solicitacao);
        var consumer = new ProcessarSolicitacaoCertificadosConsumer(
            repository,
            new FakeCursoRepository(curso),
            new FakeGeradorPdfCertificado(),
            new FakeArmazenamentoCertificadoPdf(),
            NullLogger<ProcessarSolicitacaoCertificadosConsumer>.Instance);

        await consumer.ProcessarAsync(solicitacao.Id, CancellationToken.None);

        Assert.AreEqual(StatusSolicitacaoCertificado.Falha, solicitacao.Status);
    }

    [TestMethod]
    public async Task Consume_ComSolicitacaoInexistente_TrataComSeguranca()
    {
        var repository = new FakeSolicitacaoCertificadoRepository(null);
        var consumer = new ProcessarSolicitacaoCertificadosConsumer(
            repository,
            new FakeCursoRepository(null),
            new FakeGeradorPdfCertificado(),
            new FakeArmazenamentoCertificadoPdf(),
            NullLogger<ProcessarSolicitacaoCertificadosConsumer>.Instance);

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

    private sealed class FakeCursoRepository(Curso? curso) : IRepositorioCurso
    {
        public Task AdicionarAsync(Curso curso, CancellationToken ct) => Task.CompletedTask;

        public Task<Curso?> ObterPorIdAsync(Guid id, CancellationToken ct) => Task.FromResult(curso);
    }

    private sealed class FakeArmazenamentoCertificadoPdf(bool arquivoExiste = false) : IArmazenamentoCertificadoPdf
    {
        public int QuantidadeSalvamentos { get; private set; }

        public string ObterDiretorioBase() => "C:/tmp/certificados";

        public string ObterCaminhoRelativo(Guid solicitacaoId, Guid certificadoId) => $"certificados/solicitacao/{certificadoId}.pdf";

        public string ObterCaminhoFisico(Guid solicitacaoId, Guid certificadoId) => $"C:/tmp/certificados/solicitacao/{certificadoId}.pdf";

        public void Salvar(Guid solicitacaoId, Guid certificadoId, byte[] pdf)
        {
            QuantidadeSalvamentos++;
        }

        public bool ArquivoExiste(Guid solicitacaoId, Guid certificadoId) => arquivoExiste;
    }

    private sealed class FakeGeradorPdfCertificado(string? gerarFalhaPara = null) : IGeradorPdfCertificado
    {
        public int QuantidadeChamadas { get; private set; }

        public byte[] GerarPdf(DadosCertificadoPdf dados)
        {
            QuantidadeChamadas++;
            if (gerarFalhaPara is not null && dados.NomeAluno == gerarFalhaPara)
                throw new InvalidOperationException("Falha simulada");

            return new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D };
        }
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
