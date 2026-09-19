using GeradorCertificados.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace GeradorCertificados.Infrastructure.Persistencia;
public sealed class AplicacaoDbContext(DbContextOptions<AplicacaoDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e => { e.ToTable("Usuarios"); e.HasKey(x => x.Id); e.Property(x => x.Email).HasMaxLength(320).IsRequired(); e.HasIndex(x => x.Email).IsUnique(); e.Property(x => x.SenhaHash).IsRequired(); });
    }
}
