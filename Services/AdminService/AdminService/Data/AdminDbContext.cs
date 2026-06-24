using Microsoft.EntityFrameworkCore;
using AdminService.Data.Entities;

namespace AdminService.Data;

public class AdminDbContext : DbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { }

    public DbSet<MovieFileEntity> MovieFiles => Set<MovieFileEntity>();
    public DbSet<DownloadJobEntity> DownloadJobs => Set<DownloadJobEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MovieFileEntity>(entity =>
        {
            entity.ToTable("MovieFiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).HasMaxLength(500);
            entity.Property(e => e.FilePath).HasMaxLength(1000);
            entity.HasIndex(e => e.TmdbId);
        });

        modelBuilder.Entity<DownloadJobEntity>(entity =>
        {
            entity.ToTable("DownloadJobs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MagnetUrl).HasMaxLength(2000);
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("downloading");
            entity.HasIndex(e => e.TmdbId);
        });
    }
}
