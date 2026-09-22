using GeradorCertificados.Application.Cursos;
using GeradorCertificados.Domain.Certificados;
using MediatR;

namespace GeradorCertificados.Application.Certificados.Queries;

public sealed class ConsultarStatusSolicitacaoQueryHandler(
    IRepositorioCurso cursos,
    ISolicitacaoCertificadoRepository solicitacoes) : IRequestHandler<ConsultarStatusSolicitacaoQuery, StatusSolicitacaoResponse>
{
    public async Task<StatusSolicitacaoResponse> Handle(ConsultarStatusSolicitacaoQuery request, CancellationToken ct)
    {
        var curso = await cursos.ObterPorIdAsync(request.CursoId, ct);
        if (curso is null)
            throw new CursoInexistenteException(request.CursoId);

        var solicitacao = await solicitacoes.ObterMaisRecentePorCursoAsync(request.CursoId, ct);
        if (solicitacao is null)
            throw new SolicitacaoNaoEncontradaException(request.CursoId);

        return new StatusSolicitacaoResponse(
            solicitacao.Id,
            solicitacao.CursoId,
            solicitacao.Status,
            solicitacao.DataSolicitacao,
            solicitacao.Certificados.Count,
            solicitacao.Certificados.Count(x => x.Status == StatusCertificado.Gerado),
            solicitacao.Certificados.Count(x => x.Status == StatusCertificado.Falha));
    }
}

public sealed record StatusSolicitacaoResponse(
    Guid SolicitacaoId,
    Guid CursoId,
    StatusSolicitacaoCertificado Status,
    DateTimeOffset DataSolicitacao,
    int QuantidadeTotal,
    int QuantidadeGerada,
    int QuantidadeFalha);
