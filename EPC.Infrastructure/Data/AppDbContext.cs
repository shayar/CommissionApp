using Microsoft.EntityFrameworkCore;
using EPC.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using EPC.Infrastructure.Identity;

namespace EPC.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<SubCategory> SubCategories => Set<SubCategory>();
        public DbSet<Sale> Sales => Set<Sale>();

        public DbSet<Store> Stores => Set<Store>();
        public DbSet<Audit> AuditLogs => Set<Audit>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<AppUser>().HasIndex(e => e.Email).IsUnique();

            modelBuilder.Entity<Sale>()
    .HasOne<AppUser>()
    .WithMany(u => u.Sales)
    .HasForeignKey(s => s.AppUserId)
    .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
