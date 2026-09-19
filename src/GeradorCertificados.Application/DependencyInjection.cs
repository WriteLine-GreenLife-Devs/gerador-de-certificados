using Microsoft.Extensions.DependencyInjection;
using GeradorCertificados.Application.Usuarios;
using GeradorCertificados.Application.Cursos;

namespace GeradorCertificados.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AutenticacaoService>();
        services.AddScoped<ServicoCursos>();
        return services;
    }
}
