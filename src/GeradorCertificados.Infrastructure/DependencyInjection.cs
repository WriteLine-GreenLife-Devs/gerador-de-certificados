using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using GeradorCertificados.Application.Usuarios;
using GeradorCertificados.Infrastructure.Persistencia;
using GeradorCertificados.Infrastructure.Usuarios;
using GeradorCertificados.Application.Cursos;
using GeradorCertificados.Infrastructure.Cursos;
using GeradorCertificados.Application.Certificados;
using GeradorCertificados.Application.Certificados.Mensagens;
using GeradorCertificados.Infrastructure.Certificados;
using GeradorCertificados.Infrastructure.RabbitMq;
using MassTransit;

namespace GeradorCertificados.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AplicacaoDbContext>(o => o.UseNpgsql(configuration.GetConnectionString("PostgresEF")));
        services.AddScoped<IUsuarioRepository, UsuarioRepository>(); services.AddSingleton<ISenhaService, SenhaService>(); services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IRepositorioCurso, RepositorioCurso>();
        services.AddScoped<ISolicitacaoCertificadoRepository, SolicitacaoCertificadoRepository>();
        services.AddScoped<IPublicadorSolicitacaoCertificados, MassTransitCertificadosMensagemPublisher>();

        var rabbitMqOptions = configuration.GetSection("RabbitMq").Get<RabbitMqOptions>() ?? new RabbitMqOptions();
        services.AddMassTransit(x =>
        {
            x.AddConsumer<ProcessarSolicitacaoCertificadosConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqOptions.Host, h =>
                {
                    h.Username(rabbitMqOptions.Username);
                    h.Password(rabbitMqOptions.Password);
                });

                cfg.ReceiveEndpoint("processar-solicitacao-certificados", e =>
                {
                    e.ConfigureConsumer<ProcessarSolicitacaoCertificadosConsumer>(context);
                    e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(2)));
                });
            });
        });

        return services;
    }
}
