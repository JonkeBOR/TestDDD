using Microsoft.EntityFrameworkCore;
using TestDDD.Models;

namespace TestDDD.Data;

public class KycDbContext : DbContext
{
    public KycDbContext(DbContextOptions<KycDbContext> options) : base(options)
    {
    }

    public virtual DbSet<CachedKycData> CachedKycData { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CachedKycData>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<CachedKycData>()
            .HasIndex(x => x.Ssn)
            .IsUnique();

        modelBuilder.Entity<CachedKycData>()
            .Property(x => x.Ssn)
            .IsRequired()
            .HasMaxLength(20);
    }
}
