using GeradorCertificados.Application.Cursos;
using GeradorCertificados.Domain.Certificados;
using MediatR;

namespace GeradorCertificados.Application.Certificados.Queries;

public sealed class ListarCertificadosQueryHandler(
    IRepositorioCurso cursos,
    ISolicitacaoCertificadoRepository solicitacoes) : IRequestHandler<ListarCertificadosQuery, IReadOnlyCollection<CertificadoResumo>>
{
    public async Task<IReadOnlyCollection<CertificadoResumo>> Handle(ListarCertificadosQuery request, CancellationToken ct)
    {
        var curso = await cursos.ObterPorIdAsync(request.CursoId, ct);
        if (curso is null)
            throw new CursoInexistenteException(request.CursoId);

        var solicitacao = await solicitacoes.ObterMaisRecentePorCursoAsync(request.CursoId, ct);
        if (solicitacao is null)
            throw new SolicitacaoNaoEncontradaException(request.CursoId);

        return solicitacao.Certificados
            .Select(x => new CertificadoResumo(
                x.Id,
                x.NomeAluno,
                x.Status,
                x.DataGeracao))
            .ToList();
    }
}

public sealed record CertificadoResumo(Guid Id, string NomeAluno, StatusCertificado Status, DateTimeOffset? DataGeracao);
