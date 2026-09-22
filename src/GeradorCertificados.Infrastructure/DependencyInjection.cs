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
using QuestPDF.Infrastructure;
using System.Security.Authentication;

namespace GeradorCertificados.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var databaseProvider = configuration["Database:Provider"] ?? "Postgres";
        var connectionName = databaseProvider switch
        {
            "Postgres" => "PostgresEF",
            "SqlServer" => "SqlServerEF",
            _ => throw new InvalidOperationException($"Provider de banco não suportado: '{databaseProvider}'. Valores válidos: Postgres, SqlServer.")
        };
        var connectionString = configuration.GetConnectionString(connectionName);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"A connection string 'ConnectionStrings:{connectionName}' é obrigatória para o provider '{databaseProvider}'.");

        services.AddDbContext<AplicacaoDbContext>(options =>
        {
            if (databaseProvider == "Postgres")
            {
                options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsAssembly(typeof(AplicacaoDbContext).Assembly.GetName().Name));
                return;
            }

            options.UseSqlServer(connectionString, sqlServer =>
                sqlServer.MigrationsAssembly("GeradorCertificados.Infrastructure.SqlServerMigrations"));
        });
        services.AddScoped<IUsuarioRepository, UsuarioRepository>(); services.AddSingleton<ISenhaService, SenhaService>(); services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IRepositorioCurso, RepositorioCurso>();
        services.AddScoped<ISolicitacaoCertificadoRepository, SolicitacaoCertificadoRepository>();
        services.AddScoped<IPublicadorSolicitacaoCertificados, MassTransitCertificadosMensagemPublisher>();
        services.AddSingleton<IGeradorPdfCertificado, QuestPdfCertificadoGenerator>();
        services.AddSingleton<IGeradorZipCertificados>(_ =>
        {
            var diretorioBase = configuration["Certificados:DiretorioBase"] ?? Path.Combine(AppContext.BaseDirectory, "certificados");
            return new ZipCertificadosGenerator(diretorioBase);
        });
        services.AddSingleton<IArmazenamentoCertificadoPdf>(_ =>
        {
            var diretorioBase = configuration["Certificados:DiretorioBase"] ?? Path.Combine(AppContext.BaseDirectory, "certificados");
            return new ArmazenamentoCertificadoPdf(diretorioBase);
        });

        var rabbitMqOptions = configuration.GetSection("RabbitMq").Get<RabbitMqOptions>() ?? new RabbitMqOptions();
        services.AddMassTransit(x =>
        {
            x.AddConsumer<ProcessarSolicitacaoCertificadosConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqOptions.Host, rabbitMqOptions.Port, rabbitMqOptions.VirtualHost, h =>
                {
                    h.Username(rabbitMqOptions.Username);
                    h.Password(rabbitMqOptions.Password);

                    if (rabbitMqOptions.UseSsl)
                        h.UseSsl(s => s.Protocol = SslProtocols.Tls12);
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
