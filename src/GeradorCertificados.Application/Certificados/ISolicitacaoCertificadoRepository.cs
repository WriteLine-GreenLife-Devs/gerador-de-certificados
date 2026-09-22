using GeradorCertificados.Domain.Certificados;

namespace GeradorCertificados.Application.Certificados;

public interface ISolicitacaoCertificadoRepository
{
    Task AdicionarAsync(SolicitacaoCertificado solicitacao, CancellationToken ct);
    Task<SolicitacaoCertificado?> ObterPorIdAsync(Guid id, CancellationToken ct);
    Task<SolicitacaoCertificado?> ObterMaisRecentePorCursoAsync(Guid cursoId, CancellationToken ct);
    Task<IReadOnlyCollection<SolicitacaoCertificado>> ObterPorCursoAsync(Guid cursoId, CancellationToken ct);
    Task<IReadOnlyCollection<Certificado>> ObterCertificadosPorSolicitacaoAsync(Guid solicitacaoId, CancellationToken ct);
    Task SalvarAlteracoesAsync(CancellationToken ct);
}
