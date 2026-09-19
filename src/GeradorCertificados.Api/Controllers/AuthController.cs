using GeradorCertificados.Api.Contracts.Auth;
using GeradorCertificados.Application.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeradorCertificados.Api.Controllers;
[ApiController, Route("auth"), AllowAnonymous]
public sealed class AuthController(AutenticacaoService service) : ControllerBase
{
    [HttpPost("cadastro")] public async Task<IActionResult> Cadastro(CadastroRequest request, CancellationToken ct) { try { await service.CadastrarAsync(request.Email, request.Senha, ct); return StatusCode(StatusCodes.Status201Created); } catch (ConflitoDeNegocioException e) { return Conflict(new ProblemDetails { Title = "Conflito", Detail = e.Message, Status = 409 }); } catch (ArgumentException e) { return BadRequest(new ProblemDetails { Title = "Dados inválidos", Detail = e.Message, Status = 400 }); } }
    [HttpPost("login")] public async Task<ActionResult<TokenResponse>> Login(LoginRequest request, CancellationToken ct) { try { var t = await service.LoginAsync(request.Email, request.Senha, ct); return Ok(new TokenResponse(t.AccessToken, t.ExpiraEm)); } catch (CredenciaisInvalidasException e) { return Unauthorized(new ProblemDetails { Title = "Não autorizado", Detail = e.Message, Status = 401 }); } catch (ArgumentException e) { return BadRequest(new ProblemDetails { Title = "Dados inválidos", Detail = e.Message, Status = 400 }); } }
}
