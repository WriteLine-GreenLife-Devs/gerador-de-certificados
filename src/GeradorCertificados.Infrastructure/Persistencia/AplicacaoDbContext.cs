using GeradorCertificados.Domain.Certificados;
using GeradorCertificados.Domain.Cursos;
using GeradorCertificados.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace GeradorCertificados.Infrastructure.Persistencia;
public sealed class AplicacaoDbContext(DbContextOptions<AplicacaoDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<SolicitacaoCertificado> SolicitacoesCertificado => Set<SolicitacaoCertificado>();
    public DbSet<Certificado> Certificados => Set<Certificado>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e => { e.ToTable("Usuarios"); e.HasKey(x => x.Id); e.Property(x => x.Email).HasMaxLength(320).IsRequired(); e.HasIndex(x => x.Email).IsUnique(); e.Property(x => x.SenhaHash).IsRequired(); });
        modelBuilder.Entity<Curso>(e => { e.ToTable("Cursos"); e.HasKey(x => x.Id); e.Property(x => x.Nome).HasMaxLength(200).IsRequired(); e.Property(x => x.Descricao).HasMaxLength(500); e.Property(x => x.CargaHoraria).IsRequired(); e.Property(x => x.DataConclusao).IsRequired(); });

        modelBuilder.Entity<SolicitacaoCertificado>(e =>
        {
            e.ToTable("SolicitacoesCertificado");
            e.HasKey(x => x.Id);
            e.Property(x => x.CursoId).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().IsRequired();
            e.Property(x => x.DataSolicitacao).IsRequired();
            e.HasOne<Curso>()
                .WithMany()
                .HasForeignKey(x => x.CursoId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Certificados)
                .WithOne()
                .HasForeignKey(x => x.SolicitacaoId)
                .OnDelete(DeleteBehavior.Restrict);
            var filtroSolicitacaoAtiva = Database.ProviderName == "Microsoft.EntityFrameworkCore.SqlServer"
                ? "[Status] IN ('Pendente', 'GerandoCertificados', 'GerandoZip')"
                : "\"Status\" IN ('Pendente', 'GerandoCertificados', 'GerandoZip')";

            e.HasIndex(x => x.CursoId)
                .IsUnique()
                .HasFilter(filtroSolicitacaoAtiva);
        });

        modelBuilder.Entity<Certificado>(e =>
        {
            e.ToTable("Certificados");
            e.HasKey(x => x.Id);
            e.Property(x => x.SolicitacaoId).IsRequired();
            e.Property(x => x.NomeAluno).HasMaxLength(200).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().IsRequired();
            e.Property(x => x.CaminhoArquivo).IsRequired(false);
            e.Property(x => x.DataGeracao).IsRequired(false);
            e.HasOne<SolicitacaoCertificado>()
                .WithMany(x => x.Certificados)
                .HasForeignKey(x => x.SolicitacaoId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
