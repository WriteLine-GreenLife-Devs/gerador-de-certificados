using GeradorCertificados.Domain.Cursos;
namespace GeradorCertificados.Application.Cursos;
public interface IRepositorioCurso { Task AdicionarAsync(Curso curso, CancellationToken ct); Task<Curso?> ObterPorIdAsync(Guid id, CancellationToken ct); }
public sealed class ServicoCursos(IRepositorioCurso cursos)
{ public async Task<Curso> CadastrarAsync(string nome, string? descricao, int cargaHoraria, DateOnly dataConclusao, CancellationToken ct) { var curso = Curso.Criar(nome, descricao, cargaHoraria, dataConclusao); await cursos.AdicionarAsync(curso, ct); return curso; } public Task<Curso?> ConsultarAsync(Guid id, CancellationToken ct) => cursos.ObterPorIdAsync(id, ct); }
