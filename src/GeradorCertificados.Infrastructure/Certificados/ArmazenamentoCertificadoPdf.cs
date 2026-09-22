using System.Security.Cryptography;
using GeradorCertificados.Application.Certificados;

namespace GeradorCertificados.Infrastructure.Certificados;

public sealed class ArmazenamentoCertificadoPdf(string diretorioBase) : IArmazenamentoCertificadoPdf
{
    public string ObterDiretorioBase() => diretorioBase;

    public string ObterCaminhoRelativo(Guid solicitacaoId, Guid certificadoId)
        => $"certificados/{solicitacaoId}/{certificadoId}.pdf";

    public string ObterCaminhoFisico(Guid solicitacaoId, Guid certificadoId)
    {
        var caminhoBase = Path.GetFullPath(diretorioBase);
        var caminhoArquivo = Path.Combine(caminhoBase, solicitacaoId.ToString(), $"{certificadoId}.pdf");
        return Path.GetFullPath(caminhoArquivo);
    }

    public void Salvar(Guid solicitacaoId, Guid certificadoId, byte[] pdf)
    {
        ArgumentNullException.ThrowIfNull(pdf);

        var diretorioSolicitacao = Path.Combine(Path.GetFullPath(diretorioBase), solicitacaoId.ToString());
        Directory.CreateDirectory(diretorioSolicitacao);

        var caminhoCompleto = Path.Combine(diretorioSolicitacao, $"{certificadoId}.pdf");
        File.WriteAllBytes(caminhoCompleto, pdf);
    }

    public bool ArquivoExiste(Guid solicitacaoId, Guid certificadoId)
    {
        var caminhoArquivo = ObterCaminhoFisico(solicitacaoId, certificadoId);
        return File.Exists(caminhoArquivo);
    }
}
