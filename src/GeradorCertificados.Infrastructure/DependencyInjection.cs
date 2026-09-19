using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using GeradorCertificados.Application.Usuarios;
using GeradorCertificados.Infrastructure.Persistencia;
using GeradorCertificados.Infrastructure.Usuarios;

namespace GeradorCertificados.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AplicacaoDbContext>(o => o.UseSqlite(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<IUsuarioRepository, UsuarioRepository>(); services.AddSingleton<ISenhaService, SenhaService>(); services.AddSingleton<ITokenService, JwtTokenService>();
        return services;
    }
}
