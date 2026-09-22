using GeradorCertificados.Application.Certificados;
using GeradorCertificados.Domain.Certificados;
using GeradorCertificados.Domain.Cursos;
using GeradorCertificados.Infrastructure.Certificados;

namespace GeradorCertificados.Application.Tests.Modulos.Certificados;

[TestClass]
public sealed class QuestPdfCertificadoGeneratorTests
{
    [TestMethod]
    public void GerarPdf_ComDadosValidos_RetornaPdfValido()
    {
        var dados = new DadosCertificadoPdf(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Maria Silva",
            "Curso de C#",
            40,
            new DateOnly(2026, 9, 22));

        var generator = new QuestPdfCertificadoGenerator();

        var pdf = generator.GerarPdf(dados);

        Assert.IsNotNull(pdf);
        Assert.IsNotEmpty(pdf);
        Assert.AreEqual(0x25, pdf[0]);
        Assert.AreEqual(0x50, pdf[1]);
        Assert.AreEqual(0x44, pdf[2]);
        Assert.AreEqual(0x46, pdf[3]);
    }

    [TestMethod]
    public void ArmazenamentoCertificadoPdf_UsaCertificadoIdNoNomeDoArquivo()
    {
        var armazenamento = new ArmazenamentoCertificadoPdf("C:/tmp/gerador-certificados-tests");
        var solicitacaoId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var certificadoId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var caminhoRelativo = armazenamento.ObterCaminhoRelativo(solicitacaoId, certificadoId);
        var caminhoFisico = armazenamento.ObterCaminhoFisico(solicitacaoId, certificadoId);

        Assert.AreEqual("certificados/11111111-1111-1111-1111-111111111111/22222222-2222-2222-2222-222222222222.pdf", caminhoRelativo);
        StringAssert.Contains(caminhoFisico, "gerador-certificados-tests", StringComparison.OrdinalIgnoreCase);
        Assert.IsFalse(caminhoFisico.Contains("Maria Silva", StringComparison.OrdinalIgnoreCase));
    }
}
