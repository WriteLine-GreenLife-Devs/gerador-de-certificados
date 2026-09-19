using Microsoft.Extensions.DependencyInjection;

namespace GeradorCertificados.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Registros de banco, JWT, arquivos e mensageria serão incluídos pelos módulos.
        return services;
    }
}
