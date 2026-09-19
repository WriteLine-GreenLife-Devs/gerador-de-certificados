using GeradorCertificados.Domain.Usuarios;
using GeradorCertificados.Domain.Cursos;
using Microsoft.EntityFrameworkCore;

namespace GeradorCertificados.Infrastructure.Persistencia;
public sealed class AplicacaoDbContext(DbContextOptions<AplicacaoDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Curso> Cursos => Set<Curso>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e => { e.ToTable("Usuarios"); e.HasKey(x => x.Id); e.Property(x => x.Email).HasMaxLength(320).IsRequired(); e.HasIndex(x => x.Email).IsUnique(); e.Property(x => x.SenhaHash).IsRequired(); });
        modelBuilder.Entity<Curso>(e => { e.ToTable("Cursos"); e.HasKey(x => x.Id); e.Property(x => x.Nome).HasMaxLength(200).IsRequired(); e.Property(x => x.Descricao).HasMaxLength(500); e.Property(x => x.CargaHoraria).IsRequired(); e.Property(x => x.DataConclusao).IsRequired(); });
    }
}
