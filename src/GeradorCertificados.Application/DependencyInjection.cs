using Microsoft.Extensions.DependencyInjection;
using GeradorCertificados.Application.Usuarios;

namespace GeradorCertificados.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AutenticacaoService>();
        return services;
    }
}
