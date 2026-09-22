using GeradorCertificados.Application.Certificados.Commands;
using GeradorCertificados.Application.Certificados.Queries;
using GeradorCertificados.Application.Cursos;
using GeradorCertificados.Application.Usuarios;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GeradorCertificados.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AutenticacaoService>();
        services.AddScoped<ServicoCursos>();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<SolicitarGeracaoCertificadosCommandHandler>();
        services.AddScoped<ConsultarStatusSolicitacaoQueryHandler>();
        services.AddScoped<ListarCertificadosQueryHandler>();

        return services;
    }
}
