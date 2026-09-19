using GeradorCertificados.Api.Contracts.Cursos;
using GeradorCertificados.Application.Cursos;
using Microsoft.AspNetCore.Mvc;
namespace GeradorCertificados.Api.Controllers;
[ApiController, Route("cursos")]
public sealed class CursosController(ServicoCursos service) : ControllerBase
{
 [HttpPost] public async Task<IActionResult> Cadastrar(CadastrarCursoRequest request, CancellationToken ct) { try { var curso = await service.CadastrarAsync(request.Nome, request.Descricao, request.CargaHoraria, request.DataConclusao, ct); return CreatedAtAction(nameof(Consultar), new { cursoId = curso.Id }, ParaResposta(curso)); } catch (ArgumentException e) { return BadRequest(new ProblemDetails { Title = "Dados inválidos", Detail = e.Message, Status = 400 }); } }
 [HttpGet("{cursoId:guid}")] public async Task<ActionResult<CursoResponse>> Consultar(Guid cursoId, CancellationToken ct) { var curso = await service.ConsultarAsync(cursoId, ct); return curso is null ? NotFound() : Ok(ParaResposta(curso)); }
 private static CursoResponse ParaResposta(GeradorCertificados.Domain.Cursos.Curso curso) => new(curso.Id, curso.Nome, curso.Descricao, curso.CargaHoraria, curso.DataConclusao);
}
