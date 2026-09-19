using GeradorCertificados.Application.Cursos;
using GeradorCertificados.Domain.Cursos;
using GeradorCertificados.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
namespace GeradorCertificados.Infrastructure.Cursos;
public sealed class RepositorioCurso(AplicacaoDbContext db) : IRepositorioCurso
{ public async Task AdicionarAsync(Curso curso, CancellationToken ct) { db.Cursos.Add(curso); await db.SaveChangesAsync(ct); } public Task<Curso?> ObterPorIdAsync(Guid id, CancellationToken ct) => db.Cursos.SingleOrDefaultAsync(x => x.Id == id, ct); }
