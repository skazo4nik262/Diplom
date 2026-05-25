using IdentityService.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
				entity.HasKey(e => e.Id);
				entity.ToTable("Users");
				entity.Property(e => e.Login).HasMaxLength(255);
				entity.Property(e => e.PasswordHash).HasColumnName("user_password").HasMaxLength(500);
				entity.Property(e => e.Role).HasColumnName("user_role").HasDefaultValue(1);
				entity.Property(e => e.IsActive).HasDefaultValue(true);
				entity.HasIndex(e => e.Login).IsUnique();
			});
        }
    }
}
