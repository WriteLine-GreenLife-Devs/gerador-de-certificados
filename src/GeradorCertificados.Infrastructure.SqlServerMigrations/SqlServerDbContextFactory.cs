using GeradorCertificados.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GeradorCertificados.Infrastructure.SqlServerMigrations;

public sealed class SqlServerDbContextFactory : IDesignTimeDbContextFactory<AplicacaoDbContext>
{
    public AplicacaoDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("SqlServerEF")
            ?? "Server=localhost\\SQLEXPRESS;Database=GeradorCertificadosDb;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<AplicacaoDbContext>()
            .UseSqlServer(
                connectionString,
                sqlServer => sqlServer.MigrationsAssembly(typeof(SqlServerDbContextFactory).Assembly.GetName().Name))
            .Options;

        return new AplicacaoDbContext(options);
    }
}
