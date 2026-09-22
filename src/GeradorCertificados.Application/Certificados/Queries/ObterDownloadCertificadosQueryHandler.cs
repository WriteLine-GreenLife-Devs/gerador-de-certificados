using GeradorCertificados.Application.Cursos;
using GeradorCertificados.Domain.Certificados;
using MediatR;

namespace GeradorCertificados.Application.Certificados.Queries;

public sealed class ObterDownloadCertificadosQueryHandler(
    IRepositorioCurso cursos,
    ISolicitacaoCertificadoRepository solicitacoes,
    IGeradorZipCertificados zipGenerator) : IRequestHandler<ObterDownloadCertificadosQuery, ArquivoCertificadoDownload?>
{
    public async Task<ArquivoCertificadoDownload?> Handle(ObterDownloadCertificadosQuery request, CancellationToken ct)
    {
        var curso = await cursos.ObterPorIdAsync(request.CursoId, ct);
        if (curso is null)
            throw new CursoInexistenteException(request.CursoId);

        var solicitacao = await solicitacoes.ObterMaisRecentePorCursoAsync(request.CursoId, ct);
        if (solicitacao is null)
            throw new SolicitacaoNaoEncontradaException(request.CursoId);

        if (solicitacao.Status != StatusSolicitacaoCertificado.Concluido)
            return null;

        if (!zipGenerator.Existe(solicitacao.Id))
            return null;

        var zip = zipGenerator.LerZip(solicitacao.Id);
        return new ArquivoCertificadoDownload(zip, $"certificados-{solicitacao.Id}.zip", "application/zip");
    }
}
