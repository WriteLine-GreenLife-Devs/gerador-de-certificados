using GeradorCertificados.Application.Certificados;
using GeradorCertificados.Domain.Certificados;
using GeradorCertificados.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace GeradorCertificados.Infrastructure.Certificados;

public sealed class SolicitacaoCertificadoRepository(AplicacaoDbContext db) : ISolicitacaoCertificadoRepository
{
    public async Task AdicionarAsync(SolicitacaoCertificado solicitacao, CancellationToken ct)
    {
        db.SolicitacoesCertificado.Add(solicitacao);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new SolicitacaoEmProcessamentoException(solicitacao.CursoId);
        }
    }

    public Task<SolicitacaoCertificado?> ObterPorIdAsync(Guid id, CancellationToken ct)
        => db.SolicitacoesCertificado
            .Include(x => x.Certificados)
            .SingleOrDefaultAsync(x => x.Id == id, ct);

    public Task<SolicitacaoCertificado?> ObterMaisRecentePorCursoAsync(Guid cursoId, CancellationToken ct)
        => db.SolicitacoesCertificado
            .Include(x => x.Certificados)
            .Where(x => x.CursoId == cursoId)
            .OrderByDescending(x => x.DataSolicitacao)
            .FirstOrDefaultAsync(ct);

    public Task<IReadOnlyCollection<SolicitacaoCertificado>> ObterPorCursoAsync(Guid cursoId, CancellationToken ct)
        => db.SolicitacoesCertificado
            .Include(x => x.Certificados)
            .Where(x => x.CursoId == cursoId)
            .OrderByDescending(x => x.DataSolicitacao)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyCollection<SolicitacaoCertificado>)t.Result, ct);

    public Task<IReadOnlyCollection<Certificado>> ObterCertificadosPorSolicitacaoAsync(Guid solicitacaoId, CancellationToken ct)
        => db.Certificados
            .Where(x => x.SolicitacaoId == solicitacaoId)
            .OrderBy(x => x.NomeAluno)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyCollection<Certificado>)t.Result, ct);

    public Task SalvarAlteracoesAsync(CancellationToken ct)
        => db.SaveChangesAsync(ct);

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException is not null
            && ex.InnerException.Message.Contains("IX_SolicitacoesCertificado_CursoId", StringComparison.OrdinalIgnoreCase);
    }
}
