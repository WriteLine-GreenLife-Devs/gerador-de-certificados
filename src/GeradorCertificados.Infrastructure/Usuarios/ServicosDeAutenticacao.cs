using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GeradorCertificados.Application.Usuarios;
using GeradorCertificados.Domain.Usuarios;
using GeradorCertificados.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GeradorCertificados.Infrastructure.Usuarios;
public sealed class UsuarioRepository(AplicacaoDbContext db) : IUsuarioRepository
{ public Task<bool> ExisteEmailAsync(string email, CancellationToken ct) => db.Usuarios.AnyAsync(x => x.Email == email, ct); public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct) => db.Usuarios.SingleOrDefaultAsync(x => x.Email == email, ct); public async Task AdicionarAsync(Usuario usuario, CancellationToken ct) { db.Usuarios.Add(usuario); await db.SaveChangesAsync(ct); } }
public sealed class SenhaService : ISenhaService
{ private readonly PasswordHasher<Usuario> _hasher = new(); public string GerarHash(Usuario usuario, string senha) => _hasher.HashPassword(usuario, senha); public bool Verificar(Usuario usuario, string senha) => _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senha) != PasswordVerificationResult.Failed; }
public sealed class JwtSettings { public string Issuer { get; init; } = "GeradorCertificados"; public string Audience { get; init; } = "GeradorCertificados"; public string SigningKey { get; init; } = null!; }
public sealed class JwtTokenService(IConfiguration config) : ITokenService
{ public string Gerar(Usuario usuario) { var s = config.GetRequiredSection("Jwt").Get<JwtSettings>()!; var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(s.SigningKey)); var token = new JwtSecurityToken(s.Issuer, s.Audience, [new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()), new Claim(ClaimTypes.Email, usuario.Email)], expires: DateTime.UtcNow.AddHours(1), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)); return new JwtSecurityTokenHandler().WriteToken(token); } }
