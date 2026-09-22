using System.IO.Compression;
using GeradorCertificados.Application.Certificados;
using GeradorCertificados.Application.Cursos;
using GeradorCertificados.Domain.Certificados;
using GeradorCertificados.Domain.Cursos;
using GeradorCertificados.Infrastructure.Certificados;
using Microsoft.Extensions.Logging.Abstractions;

namespace GeradorCertificados.Application.Tests.Modulos.Certificados;

[TestClass]
public sealed class ZipCertificadosTests
{
    [TestMethod]
    public async Task Consume_ComTodosCertificadosEmFalha_MarcaSolicitacaoComoFalha()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var solicitacao = SolicitacaoCertificado.Criar(curso.Id, ["Maria", "José"]);
        var repository = new FakeSolicitacaoCertificadoRepository(solicitacao);
        var cursoRepository = new FakeCursoRepository(curso);
        var storage = new FakeArmazenamentoCertificadoPdf();
        var generator = new FakeGeradorPdfCertificado(gerarFalhaPara: "Maria", gerarFalhaPara2: "José");
        var zipGenerator = new FakeGeradorZipCertificados();
        var consumer = new ProcessarSolicitacaoCertificadosConsumer(repository, cursoRepository, generator, storage, NullLogger<ProcessarSolicitacaoCertificadosConsumer>.Instance, zipGenerator);

        await consumer.ProcessarAsync(solicitacao.Id, CancellationToken.None);

        Assert.AreEqual(StatusSolicitacaoCertificado.Falha, solicitacao.Status);
        Assert.AreEqual(2, solicitacao.Certificados.Count(x => x.Status == StatusCertificado.Falha));
        Assert.IsFalse(zipGenerator.Existe(solicitacao.Id));
    }

    [TestMethod]
    public async Task Consume_ComSolicitacaoGerandoZip_CriaZipComPdfValidoEConclui()
    {
        var curso = Curso.Criar("Curso", "Descricao", 40, new DateOnly(2026, 1, 1));
        var solicitacao = SolicitacaoCertificado.Criar(curso.Id, ["Maria"]);
        solicitacao.IniciarProcessamento();
        solicitacao.Certificados.Single().MarcarComoGerado("certificados/solicitacao/arquivo.pdf");
        solicitacao.IniciarGeracaoZip();

        var repository = new FakeSolicitacaoCertificadoRepository(solicitacao);
        var cursoRepository = new FakeCursoRepository(curso);
        var storage = new FakeArmazenamentoCertificadoPdf();
        var generator = new FakeGeradorPdfCertificado();
        var zipGenerator = new FakeGeradorZipCertificados();
        var consumer = new ProcessarSolicitacaoCertificadosConsumer(repository, cursoRepository, generator, storage, NullLogger<ProcessarSolicitacaoCertificadosConsumer>.Instance, zipGenerator);

        await consumer.ProcessarAsync(solicitacao.Id, CancellationToken.None);

        Assert.AreEqual(StatusSolicitacaoCertificado.Concluido, solicitacao.Status);
        Assert.IsTrue(zipGenerator.Existe(solicitacao.Id));
        Assert.AreEqual(1, zipGenerator.QuantidadeChamadas);
    }

    [TestMethod]
    public void GeradorZip_GeraArquivoValidoComSomenteCertificadosGerados()
    {
        var solicitacaoId = Guid.NewGuid();
        var zipGenerator = new ZipCertificadosGenerator("C:/tmp/gerador-zip-tests");
        var certificados = new[] { Guid.NewGuid(), Guid.NewGuid() };

        Directory.CreateDirectory(Path.Combine("C:/tmp/gerador-zip-tests", solicitacaoId.ToString()));
        File.WriteAllBytes(Path.Combine("C:/tmp/gerador-zip-tests", solicitacaoId.ToString(), $"{certificados[0]}.pdf"), new byte[] { 1, 2, 3 });
        File.WriteAllBytes(Path.Combine("C:/tmp/gerador-zip-tests", solicitacaoId.ToString(), $"{certificados[1]}.pdf"), new byte[] { 4, 5, 6 });

        zipGenerator.GerarZip(solicitacaoId, certificados);

        var zipPath = Path.Combine("C:/tmp/gerador-zip-tests", solicitacaoId.ToString(), "certificados.zip");
        Assert.IsTrue(File.Exists(zipPath));
        using var archive = ZipFile.OpenRead(zipPath);
        Assert.HasCount(2, archive.Entries);
        CollectionAssert.AreEquivalent(new[] { $"{certificados[0]}.pdf", $"{certificados[1]}.pdf" }, archive.Entries.Select(x => x.FullName).ToArray());
    }

    private sealed class FakeCursoRepository(Curso? curso) : IRepositorioCurso
    {
        public Task AdicionarAsync(Curso curso, CancellationToken ct) => Task.CompletedTask;
        public Task<Curso?> ObterPorIdAsync(Guid id, CancellationToken ct) => Task.FromResult(curso);
    }

    private sealed class FakeArmazenamentoCertificadoPdf : IArmazenamentoCertificadoPdf
    {
        public string ObterDiretorioBase() => "C:/tmp/certificados";
        public string ObterCaminhoRelativo(Guid solicitacaoId, Guid certificadoId) => $"certificados/{solicitacaoId}/{certificadoId}.pdf";
        public string ObterCaminhoFisico(Guid solicitacaoId, Guid certificadoId) => Path.Combine("C:/tmp/certificados", solicitacaoId.ToString(), $"{certificadoId}.pdf");
        public void Salvar(Guid solicitacaoId, Guid certificadoId, byte[] pdf) { }
        public bool ArquivoExiste(Guid solicitacaoId, Guid certificadoId) => false;
    }

    private sealed class FakeGeradorPdfCertificado(string? gerarFalhaPara = null, string? gerarFalhaPara2 = null) : IGeradorPdfCertificado
    {
        public byte[] GerarPdf(DadosCertificadoPdf dados)
        {
            if (gerarFalhaPara is not null && dados.NomeAluno == gerarFalhaPara)
                throw new InvalidOperationException("Falha simulada");

            if (gerarFalhaPara2 is not null && dados.NomeAluno == gerarFalhaPara2)
                throw new InvalidOperationException("Falha simulada");

            return new byte[] { 1, 2, 3, 4 };
        }
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

    private sealed class FakeGeradorZipCertificados : IGeradorZipCertificados
    {
        public int QuantidadeChamadas { get; private set; }
        private readonly HashSet<Guid> _solicitacoes = [];

        public string ObterCaminhoRelativo(Guid solicitacaoId) => $"certificados/{solicitacaoId}/certificados.zip";
        public string ObterCaminhoFisico(Guid solicitacaoId) => Path.Combine("C:/tmp/gerador-zip-tests", solicitacaoId.ToString(), "certificados.zip");
        public bool Existe(Guid solicitacaoId) => _solicitacoes.Contains(solicitacaoId);
        public void GerarZip(Guid solicitacaoId, IEnumerable<Guid> certificadosGerados)
        {
            QuantidadeChamadas++;
            _solicitacoes.Add(solicitacaoId);
        }
        public byte[] LerZip(Guid solicitacaoId) => Array.Empty<byte>();
    }
}
