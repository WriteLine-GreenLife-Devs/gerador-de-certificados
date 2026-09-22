using GeradorCertificados.Application.Certificados;
using GeradorCertificados.Application.Certificados.Commands;
using GeradorCertificados.Application.Certificados.Queries;
using GeradorCertificados.Application.Cursos;
using GeradorCertificados.Domain.Certificados;
using GeradorCertificados.Domain.Cursos;
using Moq;

namespace GeradorCertificados.Application.Tests.Modulos.Certificados;

[TestClass]
public sealed class CertificadosHandlersTests
{
    [TestMethod]
    public async Task SolicitarGeracaoCertificadosCommandHandler_ComCursoValido_CriaSolicitacaoPendente()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var cursoRepository = new FakeCursoRepository(curso);
        var solicitacaoRepository = new FakeSolicitacaoCertificadoRepository();
        var handler = new SolicitarGeracaoCertificadosCommandHandler(cursoRepository, solicitacaoRepository);

        var resultado = await handler.Handle(new SolicitarGeracaoCertificadosCommand(curso.Id, ["Maria", "José"]), CancellationToken.None);

        Assert.AreEqual(StatusSolicitacaoCertificado.Pendente, resultado.Status);
        Assert.HasCount(1, solicitacaoRepository.Adicionados);
        Assert.HasCount(2, solicitacaoRepository.Adicionados.Single().Certificados);
        Assert.HasCount(2, solicitacaoRepository.Adicionados.Single().Certificados.Where(x => x.Status == StatusCertificado.Pendente));
    }

    [TestMethod]
    public async Task SolicitarGeracaoCertificadosCommandHandler_ComListaVazia_LancaArgumentException()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var handler = new SolicitarGeracaoCertificadosCommandHandler(new FakeCursoRepository(curso), new FakeSolicitacaoCertificadoRepository());

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => handler.Handle(new SolicitarGeracaoCertificadosCommand(curso.Id, Array.Empty<string>()), CancellationToken.None));
    }

    [TestMethod]
    public async Task SolicitarGeracaoCertificadosCommandHandler_ComCursoInexistente_LancaCursoInexistenteException()
    {
        var handler = new SolicitarGeracaoCertificadosCommandHandler(new FakeCursoRepository(null), new FakeSolicitacaoCertificadoRepository());

        await Assert.ThrowsExactlyAsync<CursoInexistenteException>(() => handler.Handle(new SolicitarGeracaoCertificadosCommand(Guid.NewGuid(), ["Maria"]), CancellationToken.None));
    }

    [TestMethod]
    public async Task SolicitarGeracaoCertificadosCommandHandler_ComCursoInexistenteNoMock_NaoPersisteNemPublica()
    {
        var cursoRepositoryMock = new Mock<IRepositorioCurso>();
        cursoRepositoryMock
            .Setup(x => x.ObterPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Curso?)null);
        var solicitacaoRepositoryMock = new Mock<ISolicitacaoCertificadoRepository>();
        var publicadorMock = new Mock<IPublicadorSolicitacaoCertificados>();
        var handler = new SolicitarGeracaoCertificadosCommandHandler(
            cursoRepositoryMock.Object,
            solicitacaoRepositoryMock.Object,
            publicadorMock.Object);

        await Assert.ThrowsExactlyAsync<CursoInexistenteException>(() =>
            handler.Handle(new SolicitarGeracaoCertificadosCommand(Guid.NewGuid(), ["Maria"]), CancellationToken.None));

        solicitacaoRepositoryMock.Verify(x => x.AdicionarAsync(It.IsAny<SolicitacaoCertificado>(), It.IsAny<CancellationToken>()), Times.Never);
        publicadorMock.Verify(x => x.PublicarSolicitacaoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task SolicitarGeracaoCertificadosCommandHandler_ComSolicitacaoAtiva_LancaConflitoDeNegocio()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var solicitacaoAtiva = SolicitacaoCertificado.Criar(curso.Id, ["Maria"]);
        var handler = new SolicitarGeracaoCertificadosCommandHandler(new FakeCursoRepository(curso), new FakeSolicitacaoCertificadoRepository(solicitacaoAtiva));

        await Assert.ThrowsExactlyAsync<SolicitacaoEmProcessamentoException>(() => handler.Handle(new SolicitarGeracaoCertificadosCommand(curso.Id, ["João"]), CancellationToken.None));
    }

    [TestMethod]
    public async Task SolicitarGeracaoCertificadosCommandHandler_ComSolicitacaoAtivaNoMock_NaoPersisteNemPublica()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var solicitacaoAtiva = SolicitacaoCertificado.Criar(curso.Id, ["Maria"]);
        var cursoRepositoryMock = new Mock<IRepositorioCurso>();
        cursoRepositoryMock
            .Setup(x => x.ObterPorIdAsync(curso.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(curso);
        var solicitacaoRepositoryMock = new Mock<ISolicitacaoCertificadoRepository>();
        solicitacaoRepositoryMock
            .Setup(x => x.ObterMaisRecentePorCursoAsync(curso.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(solicitacaoAtiva);
        var publicadorMock = new Mock<IPublicadorSolicitacaoCertificados>();
        var handler = new SolicitarGeracaoCertificadosCommandHandler(
            cursoRepositoryMock.Object,
            solicitacaoRepositoryMock.Object,
            publicadorMock.Object);

        await Assert.ThrowsExactlyAsync<SolicitacaoEmProcessamentoException>(() =>
            handler.Handle(new SolicitarGeracaoCertificadosCommand(curso.Id, ["João"]), CancellationToken.None));

        solicitacaoRepositoryMock.Verify(x => x.AdicionarAsync(It.IsAny<SolicitacaoCertificado>(), It.IsAny<CancellationToken>()), Times.Never);
        publicadorMock.Verify(x => x.PublicarSolicitacaoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task SolicitarGeracaoCertificadosCommandHandler_ComSolicitacaoConcluida_PermiteNovaSolicitacao()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var solicitacaoConcluida = SolicitacaoCertificado.Criar(curso.Id, ["Maria"]);
        solicitacaoConcluida.IniciarProcessamento();
        solicitacaoConcluida.IniciarGeracaoZip();
        solicitacaoConcluida.Concluir();

        var handler = new SolicitarGeracaoCertificadosCommandHandler(new FakeCursoRepository(curso), new FakeSolicitacaoCertificadoRepository(solicitacaoConcluida));

        var resultado = await handler.Handle(new SolicitarGeracaoCertificadosCommand(curso.Id, ["João"]), CancellationToken.None);

        Assert.AreEqual(StatusSolicitacaoCertificado.Pendente, resultado.Status);
    }

    [TestMethod]
    public async Task SolicitarGeracaoCertificadosCommandHandler_ComSolicitacaoFalha_PermiteNovaSolicitacao()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var solicitacaoFalha = SolicitacaoCertificado.Criar(curso.Id, ["Maria"]);
        solicitacaoFalha.MarcarFalha();

        var handler = new SolicitarGeracaoCertificadosCommandHandler(new FakeCursoRepository(curso), new FakeSolicitacaoCertificadoRepository(solicitacaoFalha));

        var resultado = await handler.Handle(new SolicitarGeracaoCertificadosCommand(curso.Id, ["João"]), CancellationToken.None);

        Assert.AreEqual(StatusSolicitacaoCertificado.Pendente, resultado.Status);
    }

    [TestMethod]
    public async Task ConsultarStatusSolicitacaoQueryHandler_ComSolicitacaoRecente_RetornaStatusCorreto()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var solicitacao = SolicitacaoCertificado.Criar(curso.Id, ["Maria", "José"]);
        solicitacao.IniciarProcessamento();
        var handler = new ConsultarStatusSolicitacaoQueryHandler(new FakeCursoRepository(curso), new FakeSolicitacaoCertificadoRepository(solicitacao));

        var resultado = await handler.Handle(new ConsultarStatusSolicitacaoQuery(curso.Id), CancellationToken.None);

        Assert.AreEqual(solicitacao.Id, resultado.SolicitacaoId);
        Assert.AreEqual(StatusSolicitacaoCertificado.GerandoCertificados, resultado.Status);
        Assert.AreEqual(2, resultado.QuantidadeTotal);
        Assert.AreEqual(0, resultado.QuantidadeGerada);
        Assert.AreEqual(0, resultado.QuantidadeFalha);
    }

    [TestMethod]
    public async Task ConsultarStatusSolicitacaoQueryHandler_ComCursoSemSolicitacao_LancaSolicitacaoNaoEncontradaException()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var handler = new ConsultarStatusSolicitacaoQueryHandler(new FakeCursoRepository(curso), new FakeSolicitacaoCertificadoRepository());

        await Assert.ThrowsExactlyAsync<SolicitacaoNaoEncontradaException>(() => handler.Handle(new ConsultarStatusSolicitacaoQuery(curso.Id), CancellationToken.None));
    }

    [TestMethod]
    public async Task ListarCertificadosQueryHandler_ComSolicitacaoExistente_NaoExpoeCaminhoArquivo()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var solicitacao = SolicitacaoCertificado.Criar(curso.Id, ["Maria"]);
        var handler = new ListarCertificadosQueryHandler(new FakeCursoRepository(curso), new FakeSolicitacaoCertificadoRepository(solicitacao));

        var resultado = await handler.Handle(new ListarCertificadosQuery(curso.Id), CancellationToken.None);

        Assert.HasCount(1, resultado);
        Assert.AreEqual("Maria", resultado.Single().NomeAluno);
        Assert.IsFalse(resultado.Single().GetType().GetProperties().Any(p => p.Name == "CaminhoArquivo"));
    }

    [TestMethod]
    public async Task ListarCertificadosQueryHandler_ComCursoSemSolicitacao_LancaSolicitacaoNaoEncontradaException()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var handler = new ListarCertificadosQueryHandler(new FakeCursoRepository(curso), new FakeSolicitacaoCertificadoRepository());

        await Assert.ThrowsExactlyAsync<SolicitacaoNaoEncontradaException>(() => handler.Handle(new ListarCertificadosQuery(curso.Id), CancellationToken.None));
    }

    private sealed class FakeCursoRepository(Curso? curso) : IRepositorioCurso
    {
        public Task AdicionarAsync(Curso curso, CancellationToken ct) => Task.CompletedTask;

        public Task<Curso?> ObterPorIdAsync(Guid id, CancellationToken ct) => Task.FromResult(curso);
    }

    private sealed class FakeSolicitacaoCertificadoRepository(SolicitacaoCertificado? solicitacao = null) : ISolicitacaoCertificadoRepository
    {
        public List<SolicitacaoCertificado> Adicionados { get; } = [];

        public Task AdicionarAsync(SolicitacaoCertificado solicitacao, CancellationToken ct)
        {
            Adicionados.Add(solicitacao);
            return Task.CompletedTask;
        }

        public Task<SolicitacaoCertificado?> ObterPorIdAsync(Guid id, CancellationToken ct)
            => Task.FromResult(solicitacao);

        public Task<SolicitacaoCertificado?> ObterMaisRecentePorCursoAsync(Guid cursoId, CancellationToken ct)
            => Task.FromResult(solicitacao);

        public Task<IReadOnlyCollection<SolicitacaoCertificado>> ObterPorCursoAsync(Guid cursoId, CancellationToken ct)
            => Task.FromResult<IReadOnlyCollection<SolicitacaoCertificado>>(new[] { solicitacao! });

        public Task<IReadOnlyCollection<Certificado>> ObterCertificadosPorSolicitacaoAsync(Guid solicitacaoId, CancellationToken ct)
            => Task.FromResult<IReadOnlyCollection<Certificado>>(solicitacao!.Certificados.ToList());

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
