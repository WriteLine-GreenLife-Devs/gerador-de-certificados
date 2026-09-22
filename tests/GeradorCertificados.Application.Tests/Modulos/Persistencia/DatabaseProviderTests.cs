using GeradorCertificados.Infrastructure;
using GeradorCertificados.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GeradorCertificados.Application.Tests.Modulos.Persistencia;

[TestClass]
public sealed class DatabaseProviderTests
{
    [TestMethod]
    public void AddInfrastructure_SemProviderExplicito_UsaPostgres()
    {
        var services = CriarServices(("ConnectionStrings:PostgresEF", "Host=localhost;Database=postgres"));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AplicacaoDbContext>();

        Assert.AreEqual("Npgsql.EntityFrameworkCore.PostgreSQL", db.Database.ProviderName);
        Assert.AreEqual(
            "GeradorCertificados.Infrastructure",
            db.GetService<IMigrationsAssembly>().Assembly.GetName().Name);
    }

    [TestMethod]
    public void AddInfrastructure_ComProviderPostgres_UsaPostgresEF()
    {
        var services = CriarServices(
            ("Database:Provider", "Postgres"),
            ("ConnectionStrings:PostgresEF", "Host=postgres-provider-test;Database=postgres"));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AplicacaoDbContext>();

        Assert.AreEqual("Npgsql.EntityFrameworkCore.PostgreSQL", db.Database.ProviderName);
        StringAssert.Contains(db.Database.GetConnectionString()!, "Host=postgres-provider-test");
    }

    [TestMethod]
    public void AddInfrastructure_ComProviderSqlServer_UsaSqlServerEF()
    {
        var services = CriarServices(
            ("Database:Provider", "SqlServer"),
            ("ConnectionStrings:SqlServerEF", "Server=sqlserver-provider-test;Database=GeradorCertificados"));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AplicacaoDbContext>();

        Assert.AreEqual("Microsoft.EntityFrameworkCore.SqlServer", db.Database.ProviderName);
        StringAssert.Contains(db.Database.GetConnectionString()!, "Data Source=sqlserver-provider-test");
        Assert.AreEqual(
            "GeradorCertificados.Infrastructure.SqlServerMigrations",
            db.GetService<IMigrationsAssembly>().Assembly.GetName().Name);
    }

    [TestMethod]
    public void AddInfrastructure_ComProviderDesconhecido_LancaInvalidOperationException()
    {
        var services = new ServiceCollection();
        var configuration = CriarConfiguration(
            ("Database:Provider", "Oracle"),
            ("ConnectionStrings:PostgresEF", "Host=localhost;Database=postgres"));

        Assert.ThrowsExactly<InvalidOperationException>(() => services.AddInfrastructure(configuration));
    }

    [TestMethod]
    public void AddInfrastructure_ComProviderPostgresSemConnectionString_LancaInvalidOperationException()
    {
        var services = new ServiceCollection();
        var configuration = CriarConfiguration(("Database:Provider", "Postgres"));

        Assert.ThrowsExactly<InvalidOperationException>(() => services.AddInfrastructure(configuration));
    }

    [TestMethod]
    public void AddInfrastructure_ComProviderSqlServerSemConnectionString_LancaInvalidOperationException()
    {
        var services = new ServiceCollection();
        var configuration = CriarConfiguration(("Database:Provider", "SqlServer"));

        Assert.ThrowsExactly<InvalidOperationException>(() => services.AddInfrastructure(configuration));
    }

    [TestMethod]
    public void AddInfrastructure_ComProviderPostgresSemSqlServerEF_ContinuaConfigurandoPostgres()
    {
        var services = CriarServices(
            ("Database:Provider", "Postgres"),
            ("ConnectionStrings:PostgresEF", "Host=postgres-provider-test;Database=postgres"));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AplicacaoDbContext>();

        Assert.AreEqual("Npgsql.EntityFrameworkCore.PostgreSQL", db.Database.ProviderName);
    }

    [TestMethod]
    public void AddInfrastructure_ComProviderSqlServerSemPostgresEF_ContinuaConfigurandoSqlServer()
    {
        var services = CriarServices(
            ("Database:Provider", "SqlServer"),
            ("ConnectionStrings:SqlServerEF", "Server=sqlserver-provider-test;Database=GeradorCertificados"));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AplicacaoDbContext>();

        Assert.AreEqual("Microsoft.EntityFrameworkCore.SqlServer", db.Database.ProviderName);
    }

    [TestMethod]
    public void AddInfrastructure_ComProviderPostgres_UsaFiltroPostgreSql()
    {
        var services = CriarServices(
            ("Database:Provider", "Postgres"),
            ("ConnectionStrings:PostgresEF", "Host=postgres-provider-test;Database=postgres"));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AplicacaoDbContext>();
        var index = db.Model.FindEntityType(typeof(Domain.Certificados.SolicitacaoCertificado))!
            .GetIndexes()
            .Single(x => x.Properties.Single().Name == nameof(Domain.Certificados.SolicitacaoCertificado.CursoId));

        Assert.AreEqual("\"Status\" IN ('Pendente', 'GerandoCertificados', 'GerandoZip')", index.GetFilter());
    }

    [TestMethod]
    public void AddInfrastructure_ComProviderSqlServer_UsaFiltroSqlServer()
    {
        var services = CriarServices(
            ("Database:Provider", "SqlServer"),
            ("ConnectionStrings:SqlServerEF", "Server=sqlserver-provider-test;Database=GeradorCertificados"));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AplicacaoDbContext>();
        var index = db.Model.FindEntityType(typeof(Domain.Certificados.SolicitacaoCertificado))!
            .GetIndexes()
            .Single(x => x.Properties.Single().Name == nameof(Domain.Certificados.SolicitacaoCertificado.CursoId));

        Assert.AreEqual("[Status] IN ('Pendente', 'GerandoCertificados', 'GerandoZip')", index.GetFilter());
    }

    private static IServiceCollection CriarServices(params (string Key, string Value)[] values)
        => new ServiceCollection().AddInfrastructure(CriarConfiguration(values));

    private static IConfiguration CriarConfiguration(params (string Key, string Value)[] values)
    {
        var settings = values.ToDictionary(x => x.Key, x => (string?)x.Value);
        settings["RabbitMq:Host"] = "localhost";
        settings["RabbitMq:Username"] = "guest";
        settings["RabbitMq:Password"] = "guest";
        settings["RabbitMq:VirtualHost"] = "/";
        settings["RabbitMq:Port"] = "5672";
        settings["RabbitMq:UseSsl"] = "false";
        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }
}
