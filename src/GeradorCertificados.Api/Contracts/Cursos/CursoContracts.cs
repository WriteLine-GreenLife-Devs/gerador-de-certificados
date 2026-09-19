namespace GeradorCertificados.Api.Contracts.Cursos;
public sealed record CadastrarCursoRequest(string Nome, string? Descricao, int CargaHoraria, DateOnly DataConclusao);
public sealed record CursoResponse(Guid Id, string Nome, string? Descricao, int CargaHoraria, DateOnly DataConclusao);
