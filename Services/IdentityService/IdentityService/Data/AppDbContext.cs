using IdentityService.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
				entity.HasKey(e => e.Id);
				entity.ToTable("Users");
				entity.Property(e => e.Login).HasMaxLength(255);
				entity.Property(e => e.Username).HasMaxLength(255);
				entity.Property(e => e.Bio).HasMaxLength(1000);
				entity.Property(e => e.AvatarUrl).HasMaxLength(500);
				entity.Property(e => e.PasswordHash).HasColumnName("user_password").HasMaxLength(500);
				entity.Property(e => e.Role).HasColumnName("user_role").HasDefaultValue(1);
				entity.Property(e => e.IsActive).HasDefaultValue(true);
				entity.Property(e => e.NotifyNewInCollection);
				entity.Property(e => e.NotifyVideoAdded);
				entity.Property(e => e.NotifyFileAdded);
				entity.HasIndex(e => e.Login).IsUnique();
			});

            modelBuilder.Entity<RefreshTokenEntity>(entity =>
            {
                entity.ToTable("RefreshTokens");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Token).HasMaxLength(500);
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
