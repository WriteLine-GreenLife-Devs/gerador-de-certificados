using System.IO.Compression;
using GeradorCertificados.Application.Certificados;

namespace GeradorCertificados.Infrastructure.Certificados;

public sealed class ZipCertificadosGenerator(string diretorioBase) : IGeradorZipCertificados
{
    public string ObterCaminhoRelativo(Guid solicitacaoId)
        => $"certificados/{solicitacaoId}/certificados.zip";

    public string ObterCaminhoFisico(Guid solicitacaoId)
    {
        var diretorioSolicitacao = Path.Combine(Path.GetFullPath(diretorioBase), solicitacaoId.ToString());
        return Path.Combine(diretorioSolicitacao, "certificados.zip");
    }

    public bool Existe(Guid solicitacaoId)
        => File.Exists(ObterCaminhoFisico(solicitacaoId));

    public void GerarZip(Guid solicitacaoId, IEnumerable<Guid> certificadosGerados)
    {
        ArgumentNullException.ThrowIfNull(certificadosGerados);

        var ids = certificadosGerados
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        if (ids.Count == 0)
            throw new InvalidOperationException("Não há certificados gerados para empacotar.");

        var diretorioSolicitacao = Path.Combine(Path.GetFullPath(diretorioBase), solicitacaoId.ToString());
        Directory.CreateDirectory(diretorioSolicitacao);

        var caminhoTemporario = Path.Combine(diretorioSolicitacao, $"{Guid.NewGuid():N}.tmp");
        var caminhoDestino = Path.Combine(diretorioSolicitacao, "certificados.zip");

        using (var stream = File.Create(caminhoTemporario))
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: false))
        {
            foreach (var certificadoId in ids)
            {
                var caminhoArquivo = Path.Combine(diretorioSolicitacao, $"{certificadoId}.pdf");
                if (!File.Exists(caminhoArquivo))
                    continue;

                var entrada = archive.CreateEntry($"{certificadoId}.pdf");
                using var entradaStream = entrada.Open();
                using var arquivo = File.OpenRead(caminhoArquivo);
                arquivo.CopyTo(entradaStream);
            }
        }

        if (File.Exists(caminhoDestino))
            File.Delete(caminhoDestino);

        File.Move(caminhoTemporario, caminhoDestino);
    }

    public byte[] LerZip(Guid solicitacaoId)
    {
        var caminho = ObterCaminhoFisico(solicitacaoId);
        return File.Exists(caminho)
            ? File.ReadAllBytes(caminho)
            : throw new FileNotFoundException("ZIP não encontrado para a solicitação.", caminho);
    }
}
