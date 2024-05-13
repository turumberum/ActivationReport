using ActivationReport.Models;
using Microsoft.EntityFrameworkCore;

namespace ActivationReport
{
    class AppDBContext : DbContext
    {
        public DbSet<Card> Cards { get; set; } = null!;
        public DbSet<Company> Companies { get; set; } = null!;
        public DbSet<Activation> Activations { get; set; } = null!;

        public AppDBContext()
        {
            //Database.EnsureDeleted();
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Port=5433;Database=activationTest;Username=postgres;Password=NOOB12");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Activation>().HasAlternateKey(a => a.CryptoBlock);
            modelBuilder.Entity<Card>().HasOne(c => c.Company)
                                        .WithMany(c => c.Cards)
                                        .HasForeignKey(c => c.CompanyId);
        }
    }
}
