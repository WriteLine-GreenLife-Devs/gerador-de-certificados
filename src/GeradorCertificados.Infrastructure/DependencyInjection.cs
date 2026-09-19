using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using GeradorCertificados.Application.Usuarios;
using GeradorCertificados.Infrastructure.Persistencia;
using GeradorCertificados.Infrastructure.Usuarios;
using GeradorCertificados.Application.Cursos;
using GeradorCertificados.Infrastructure.Cursos;

namespace GeradorCertificados.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AplicacaoDbContext>(o => o.UseNpgsql(configuration.GetConnectionString("PostgresEF")));
        services.AddScoped<IUsuarioRepository, UsuarioRepository>(); services.AddSingleton<ISenhaService, SenhaService>(); services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IRepositorioCurso, RepositorioCurso>();
        return services;
    }
}
