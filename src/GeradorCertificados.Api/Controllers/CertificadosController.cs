using GeradorCertificados.Api.Contracts.Certificados;
using GeradorCertificados.Application.Certificados;
using GeradorCertificados.Application.Certificados.Commands;
using GeradorCertificados.Application.Certificados.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using StatusSolicitacaoResponseApi = GeradorCertificados.Api.Contracts.Certificados.StatusSolicitacaoResponse;

namespace GeradorCertificados.Api.Controllers;

[ApiController]
[Route("cursos")]
public sealed class CertificadosController(IMediator mediator) : ControllerBase
{
    [HttpPost("{cursoId:guid}/certificados")]
    public async Task<IActionResult> Solicitar(Guid cursoId, [FromBody] SolicitarCertificadosRequest request, CancellationToken ct)
    {
        try
        {
            var comando = new SolicitarGeracaoCertificadosCommand(cursoId, request.Alunos);
            var resultado = await mediator.Send(comando, ct);

            var response = new SolicitacaoCertificadoResponse(
                resultado.SolicitacaoId,
                resultado.CursoId,
                resultado.Status.ToString(),
                resultado.DataSolicitacao);

            return AcceptedAtAction(nameof(ConsultarStatus), new { cursoId }, response);
        }
        catch (CursoInexistenteException ex)
        {
            return NotFound(new ProblemDetails { Title = "Curso não encontrado", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
        }
        catch (SolicitacaoEmProcessamentoException ex)
        {
            return Conflict(new ProblemDetails { Title = "Conflito", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails { Title = "Dados inválidos", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
        }
    }

    [HttpGet("{cursoId:guid}/status")]
    public async Task<ActionResult<StatusSolicitacaoResponseApi>> ConsultarStatus(Guid cursoId, CancellationToken ct)
    {
        try
        {
            var response = await mediator.Send(new ConsultarStatusSolicitacaoQuery(cursoId), ct);
            return Ok(new StatusSolicitacaoResponseApi(
                response.SolicitacaoId,
                response.CursoId,
                response.Status.ToString(),
                response.DataSolicitacao,
                response.QuantidadeTotal,
                response.QuantidadeGerada,
                response.QuantidadeFalha));
        }
        catch (CursoInexistenteException ex)
        {
            return NotFound(new ProblemDetails { Title = "Curso não encontrado", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
        }
        catch (SolicitacaoNaoEncontradaException ex)
        {
            return NotFound(new ProblemDetails { Title = "Solicitação não encontrada", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
        }
    }

    [HttpGet("{cursoId:guid}/certificados")]
    public async Task<ActionResult<IEnumerable<CertificadoItemResponse>>> Listar(Guid cursoId, CancellationToken ct)
    {
        try
        {
            var certificados = await mediator.Send(new ListarCertificadosQuery(cursoId), ct);
            return Ok(certificados.Select(x => new CertificadoItemResponse(
                x.Id,
                x.NomeAluno,
                x.Status.ToString(),
                x.DataGeracao)).ToList());
        }
        catch (CursoInexistenteException ex)
        {
            return NotFound(new ProblemDetails { Title = "Curso não encontrado", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
        }
        catch (SolicitacaoNaoEncontradaException ex)
        {
            return NotFound(new ProblemDetails { Title = "Solicitação não encontrada", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
        }
    }
}
