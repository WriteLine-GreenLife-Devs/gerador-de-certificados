using Microsoft.Extensions.DependencyInjection;

namespace GeradorCertificados.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Registros de casos de uso e handlers serão incluídos pelos módulos.
        return services;
    }
}
