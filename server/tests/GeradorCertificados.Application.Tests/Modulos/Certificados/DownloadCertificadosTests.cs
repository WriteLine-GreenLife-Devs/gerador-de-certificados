using GeradorCertificados.Api.Controllers;
using GeradorCertificados.Application.Certificados;
using GeradorCertificados.Application.Certificados.DTOs;
using GeradorCertificados.Application.Certificados.Commands;
using GeradorCertificados.Application.Certificados.Queries;
using GeradorCertificados.Application.Cursos;
using GeradorCertificados.Domain.Certificados;
using GeradorCertificados.Domain.Cursos;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SolicitarCertificadosRequestApi = GeradorCertificados.Api.Contracts.Certificados.SolicitarCertificadosRequest;

namespace GeradorCertificados.Application.Tests.Modulos.Certificados;

[TestClass]
public sealed class DownloadCertificadosTests
{
    [TestMethod]
    public async Task Handler_ComCursoInexistente_LancaCursoInexistenteException()
    {
        var cursoRepository = new FakeCursoRepository(null);
        var solicitacaoRepository = new FakeSolicitacaoCertificadoRepository(null);
        var zipGenerator = new FakeZipGenerator();
        var handler = new ObterDownloadCertificadosQueryHandler(cursoRepository, solicitacaoRepository, zipGenerator);

        await Assert.ThrowsExactlyAsync<CursoInexistenteException>(() =>
            handler.Handle(new ObterDownloadCertificadosQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [TestMethod]
    public async Task Handler_ComCursoExistenteSemSolicitacao_LancaSolicitacaoNaoEncontradaException()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var cursoRepository = new FakeCursoRepository(curso);
        var solicitacaoRepository = new FakeSolicitacaoCertificadoRepository(null);
        var zipGenerator = new FakeZipGenerator();
        var handler = new ObterDownloadCertificadosQueryHandler(cursoRepository, solicitacaoRepository, zipGenerator);

        await Assert.ThrowsExactlyAsync<SolicitacaoNaoEncontradaException>(() =>
            handler.Handle(new ObterDownloadCertificadosQuery(curso.Id), CancellationToken.None));
    }

    [TestMethod]
    public async Task Handler_ComSolicitacaoPendente_RetornaNull()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var solicitacao = SolicitacaoCertificado.Criar(curso.Id, ["Maria"]);
        var handler = new ObterDownloadCertificadosQueryHandler(
            new FakeCursoRepository(curso),
            new FakeSolicitacaoCertificadoRepository(solicitacao),
            new FakeZipGenerator());

        var resultado = await handler.Handle(new ObterDownloadCertificadosQuery(curso.Id), CancellationToken.None);

        Assert.IsNull(resultado);
    }

    [TestMethod]
    public async Task Handler_ComSolicitacaoGerandoCertificados_RetornaNull()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var solicitacao = SolicitacaoCertificado.Criar(curso.Id, ["Maria"]);
        solicitacao.IniciarProcessamento();
        var handler = new ObterDownloadCertificadosQueryHandler(
            new FakeCursoRepository(curso),
            new FakeSolicitacaoCertificadoRepository(solicitacao),
            new FakeZipGenerator());

        var resultado = await handler.Handle(new ObterDownloadCertificadosQuery(curso.Id), CancellationToken.None);

        Assert.IsNull(resultado);
    }

    [TestMethod]
    public async Task Handler_ComSolicitacaoGerandoZip_RetornaNull()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var solicitacao = SolicitacaoCertificado.Criar(curso.Id, ["Maria"]);
        solicitacao.IniciarProcessamento();
        solicitacao.IniciarGeracaoZip();
        var handler = new ObterDownloadCertificadosQueryHandler(
            new FakeCursoRepository(curso),
            new FakeSolicitacaoCertificadoRepository(solicitacao),
            new FakeZipGenerator());

        var resultado = await handler.Handle(new ObterDownloadCertificadosQuery(curso.Id), CancellationToken.None);

        Assert.IsNull(resultado);
    }

    [TestMethod]
    public async Task Handler_ComSolicitacaoFalha_RetornaNull()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var solicitacao = SolicitacaoCertificado.Criar(curso.Id, ["Maria"]);
        solicitacao.MarcarFalha();
        var handler = new ObterDownloadCertificadosQueryHandler(
            new FakeCursoRepository(curso),
            new FakeSolicitacaoCertificadoRepository(solicitacao),
            new FakeZipGenerator());

        var resultado = await handler.Handle(new ObterDownloadCertificadosQuery(curso.Id), CancellationToken.None);

        Assert.IsNull(resultado);
    }

    [TestMethod]
    public async Task Handler_ComSolicitacaoConcluidaEZipExistente_RetornaArquivoSeguro()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var solicitacao = SolicitacaoCertificado.Criar(curso.Id, ["Maria"]);
        solicitacao.IniciarProcessamento();
        solicitacao.IniciarGeracaoZip();
        solicitacao.Concluir();

        var zipBytes = new byte[] { 0x50, 0x4B, 0x03, 0x04 };
        var zipGenerator = new FakeZipGenerator(zipBytes, true);
        var handler = new ObterDownloadCertificadosQueryHandler(
            new FakeCursoRepository(curso),
            new FakeSolicitacaoCertificadoRepository(solicitacao),
            zipGenerator);

        var resultado = await handler.Handle(new ObterDownloadCertificadosQuery(curso.Id), CancellationToken.None);

        Assert.IsNotNull(resultado);
        CollectionAssert.AreEqual(zipBytes, resultado!.Conteudo);
        Assert.AreEqual("application/zip", resultado.ContentType);
        Assert.AreEqual("Certificados - Curso.zip", resultado.NomeArquivo);
        Assert.IsFalse(resultado.NomeArquivo.Contains("/", StringComparison.Ordinal));
        Assert.IsFalse(resultado.NomeArquivo.Contains("\\", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task Handler_ComSolicitacaoConcluidaSemZip_RetornaNull()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var solicitacao = SolicitacaoCertificado.Criar(curso.Id, ["Maria"]);
        solicitacao.IniciarProcessamento();
        solicitacao.IniciarGeracaoZip();
        solicitacao.Concluir();

        var handler = new ObterDownloadCertificadosQueryHandler(
            new FakeCursoRepository(curso),
            new FakeSolicitacaoCertificadoRepository(solicitacao),
            new FakeZipGenerator(zipExiste: false));

        var resultado = await handler.Handle(new ObterDownloadCertificadosQuery(curso.Id), CancellationToken.None);

        Assert.IsNull(resultado);
    }

    [TestMethod]
    public async Task Controller_ComDownloadPermitido_RetornaFileResultComTipoSeguro()
    {
        var cursoId = Guid.NewGuid();
        var solicitacaoId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(x => x.Send(It.IsAny<ObterDownloadCertificadosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArquivoCertificadoDownload(new byte[] { 0x50, 0x4B, 0x03, 0x04 }, $"certificados-{solicitacaoId}.zip", "application/zip"));

        var controller = new CertificadosController(mediator.Object);

        var actionResult = await controller.Download(cursoId, CancellationToken.None);

        var fileResult = actionResult as FileContentResult;
        Assert.IsNotNull(fileResult);
        Assert.AreEqual("application/zip", fileResult!.ContentType);
        Assert.AreEqual($"certificados-{solicitacaoId}.zip", fileResult.FileDownloadName);
        CollectionAssert.AreEqual(new byte[] { 0x50, 0x4B, 0x03, 0x04 }, fileResult.FileContents);
    }

    [TestMethod]
    public async Task Controller_ComCursoInexistente_RetornaNotFound()
    {
        var cursoId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(x => x.Send(It.IsAny<ObterDownloadCertificadosQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CursoInexistenteException(cursoId));

        var controller = new CertificadosController(mediator.Object);

        var actionResult = await controller.Download(cursoId, CancellationToken.None);

        var notFound = actionResult as NotFoundObjectResult;
        Assert.IsNotNull(notFound);
        Assert.AreEqual(StatusCodes.Status404NotFound, notFound!.StatusCode);
    }

    [TestMethod]
    public async Task Controller_ComSolicitacaoPendente_RetornaConflict()
    {
        var cursoId = Guid.NewGuid();
        var solicitacaoId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(x => x.Send(It.IsAny<ObterDownloadCertificadosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArquivoCertificadoDownload?)null);
        mediator
            .Setup(x => x.Send(It.IsAny<ConsultarStatusSolicitacaoQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StatusSolicitacaoDto(solicitacaoId, cursoId, StatusSolicitacaoCertificado.Pendente, DateTimeOffset.UtcNow, 1, 0, 0));

        var controller = new CertificadosController(mediator.Object);

        var actionResult = await controller.Download(cursoId, CancellationToken.None);

        var conflict = actionResult as ObjectResult;
        Assert.IsNotNull(conflict);
        Assert.AreEqual(StatusCodes.Status409Conflict, conflict!.StatusCode);
        var problem = conflict.Value as ProblemDetails;
        Assert.IsNotNull(problem);
        Assert.AreEqual("Conflito", problem!.Title);
        StringAssert.Contains(problem.Detail!, "ainda não está concluída");
    }

    [TestMethod]
    public async Task Controller_ComSolicitacaoInexistente_RetornaNotFound()
    {
        var cursoId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(x => x.Send(It.IsAny<ObterDownloadCertificadosQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SolicitacaoNaoEncontradaException(cursoId));

        var controller = new CertificadosController(mediator.Object);

        var actionResult = await controller.Download(cursoId, CancellationToken.None);

        var notFound = actionResult as NotFoundObjectResult;
        Assert.IsNotNull(notFound);
        Assert.AreEqual(StatusCodes.Status404NotFound, notFound!.StatusCode);
        var problem = notFound.Value as ProblemDetails;
        Assert.IsNotNull(problem);
        Assert.AreEqual("Solicitação não encontrada", problem!.Title);
    }

    [TestMethod]
    public async Task Controller_ComSolicitacaoConcluidaSemZip_RetornaNotFound()
    {
        var cursoId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(x => x.Send(It.IsAny<ObterDownloadCertificadosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArquivoCertificadoDownload?)null);
        mediator
            .Setup(x => x.Send(It.IsAny<ConsultarStatusSolicitacaoQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StatusSolicitacaoDto(
                Guid.NewGuid(),
                cursoId,
                StatusSolicitacaoCertificado.Concluido,
                DateTimeOffset.UtcNow,
                1,
                1,
                0));

        var controller = new CertificadosController(mediator.Object);

        var actionResult = await controller.Download(cursoId, CancellationToken.None);

        var notFound = actionResult as NotFoundObjectResult;
        Assert.IsNotNull(notFound);
        Assert.AreEqual(StatusCodes.Status404NotFound, notFound!.StatusCode);
        var problem = notFound.Value as ProblemDetails;
        Assert.IsNotNull(problem);
        Assert.AreEqual("Arquivo não encontrado", problem!.Title);
        StringAssert.Contains(problem.Detail!, "ZIP");
    }

    [TestMethod]
    public async Task Controller_ComSolicitacaoFalha_RetornaConflictComMensagemEspecifica()
    {
        var cursoId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(x => x.Send(It.IsAny<ObterDownloadCertificadosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArquivoCertificadoDownload?)null);
        mediator
            .Setup(x => x.Send(It.IsAny<ConsultarStatusSolicitacaoQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StatusSolicitacaoDto(
                Guid.NewGuid(),
                cursoId,
                StatusSolicitacaoCertificado.Falha,
                DateTimeOffset.UtcNow,
                1,
                0,
                1));

        var controller = new CertificadosController(mediator.Object);

        var actionResult = await controller.Download(cursoId, CancellationToken.None);

        var conflict = actionResult as ObjectResult;
        Assert.IsNotNull(conflict);
        Assert.AreEqual(StatusCodes.Status409Conflict, conflict!.StatusCode);
        var problem = conflict.Value as ProblemDetails;
        Assert.IsNotNull(problem);
        Assert.AreEqual("Solicitação falhou", problem!.Title);
        Assert.AreEqual("Não há ZIP disponível para esta solicitação.", problem.Detail);
    }

    [TestMethod]
    public async Task Controller_ListarComCertificados_RetornaStatusENomeEData()
    {
        var cursoId = Guid.NewGuid();
        var dataGeracao = DateTimeOffset.Parse("2026-10-28T12:00:00Z");
        var certificados = new[]
        {
            new CertificadoResumo(Guid.NewGuid(), "Ana", StatusCertificado.Pendente, null),
            new CertificadoResumo(Guid.NewGuid(), "Bruno", StatusCertificado.Gerado, dataGeracao),
            new CertificadoResumo(Guid.NewGuid(), "Carla", StatusCertificado.Falha, null)
        };
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(x => x.Send(It.IsAny<ListarCertificadosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(certificados);
        var controller = new CertificadosController(mediator.Object);

        var actionResult = await controller.Listar(cursoId, CancellationToken.None);

        var ok = actionResult.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        Assert.AreEqual(StatusCodes.Status200OK, ok!.StatusCode);
        var itens = (IReadOnlyCollection<GeradorCertificados.Api.Contracts.Certificados.CertificadoItemResponse>)ok.Value!;
        Assert.HasCount(3, itens);
        CollectionAssert.AreEqual(
            new[] { "Ana", "Bruno", "Carla" },
            itens.Select(x => x.NomeAluno).ToArray());
        CollectionAssert.AreEqual(
            new[] { "Pendente", "Gerado", "Falha" },
            itens.Select(x => x.Status).ToArray());
        Assert.AreEqual(dataGeracao, itens.ElementAt(1).DataGeracao);
        Assert.IsNull(itens.ElementAt(0).DataGeracao);
        Assert.IsNull(itens.ElementAt(2).DataGeracao);
    }

    [TestMethod]
    public async Task Controller_ListarSemCertificados_RetornaListaVazia()
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(x => x.Send(It.IsAny<ListarCertificadosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CertificadoResumo>());
        var controller = new CertificadosController(mediator.Object);

        var actionResult = await controller.Listar(Guid.NewGuid(), CancellationToken.None);

        var ok = actionResult.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        Assert.AreEqual(StatusCodes.Status200OK, ok!.StatusCode);
        var itens = (IReadOnlyCollection<GeradorCertificados.Api.Contracts.Certificados.CertificadoItemResponse>)ok.Value!;
        Assert.IsEmpty(itens);
    }

    [TestMethod]
    public async Task Controller_ListarSemSolicitacao_RetornaNotFound()
    {
        var cursoId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(x => x.Send(It.IsAny<ListarCertificadosQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SolicitacaoNaoEncontradaException(cursoId));
        var controller = new CertificadosController(mediator.Object);

        var actionResult = await controller.Listar(cursoId, CancellationToken.None);

        var notFound = actionResult.Result as NotFoundObjectResult;
        Assert.IsNotNull(notFound);
        Assert.AreEqual(StatusCodes.Status404NotFound, notFound!.StatusCode);
        var problem = notFound.Value as ProblemDetails;
        Assert.IsNotNull(problem);
        Assert.AreEqual("Solicitação não encontrada", problem!.Title);
    }

    [TestMethod]
    public async Task Controller_SolicitarComSolicitacaoAtiva_RetornaConflict()
    {
        var cursoId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(x => x.Send(It.IsAny<SolicitarGeracaoCertificadosCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SolicitacaoEmProcessamentoException(cursoId));
        var controller = new CertificadosController(mediator.Object);

        var actionResult = await controller.Solicitar(
            cursoId,
            new SolicitarCertificadosRequestApi(["Maria"]),
            CancellationToken.None);

        var conflict = actionResult as ConflictObjectResult;
        Assert.IsNotNull(conflict);
        Assert.AreEqual(StatusCodes.Status409Conflict, conflict!.StatusCode);
        var problem = conflict.Value as ProblemDetails;
        Assert.IsNotNull(problem);
        Assert.AreEqual("Conflito", problem!.Title);
        Assert.AreEqual(StatusCodes.Status409Conflict, problem.Status);
    }

    private sealed class FakeCursoRepository(Curso? curso) : IRepositorioCurso
    {
        public Task<Curso?> ObterPorIdAsync(Guid id, CancellationToken ct) => Task.FromResult(curso);
        public Task AdicionarAsync(Curso curso, CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyList<Curso>> ListarAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Curso>>(curso is null ? [] : [curso]);
    }

    private sealed class FakeSolicitacaoCertificadoRepository(SolicitacaoCertificado? solicitacao) : ISolicitacaoCertificadoRepository
    {
        public Task AdicionarAsync(SolicitacaoCertificado solicitacao, CancellationToken ct) => Task.CompletedTask;
        public Task<SolicitacaoCertificado?> ObterPorIdAsync(Guid id, CancellationToken ct) => Task.FromResult(solicitacao);
        public Task<SolicitacaoCertificado?> ObterMaisRecentePorCursoAsync(Guid cursoId, CancellationToken ct) => Task.FromResult(solicitacao);
        public Task<IReadOnlyCollection<SolicitacaoCertificado>> ObterPorCursoAsync(Guid cursoId, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<SolicitacaoCertificado>>(solicitacao is null ? Array.Empty<SolicitacaoCertificado>() : new[] { solicitacao });
        public Task<IReadOnlyCollection<Certificado>> ObterCertificadosPorSolicitacaoAsync(Guid solicitacaoId, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<Certificado>>(solicitacao is null ? Array.Empty<Certificado>() : solicitacao.Certificados.ToList());
        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeZipGenerator(byte[]? dados = null, bool zipExiste = true) : IGeradorZipCertificados
    {
        public string ObterCaminhoRelativo(Guid solicitacaoId) => $"certificados/{solicitacaoId}/certificados.zip";
        public string ObterCaminhoFisico(Guid solicitacaoId) => Path.Combine("C:/tmp", solicitacaoId.ToString(), "certificados.zip");
        public bool Existe(Guid solicitacaoId) => zipExiste;
        public void GerarZip(Guid solicitacaoId, IEnumerable<CertificadoParaZip> certificadosGerados) { }
        public byte[] LerZip(Guid solicitacaoId) => dados ?? Array.Empty<byte>();
    }
}
