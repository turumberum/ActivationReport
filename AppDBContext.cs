using ActivationReport.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace ActivationReport
{
    class AppDBContext : DbContext
    {
        public DbSet<Card> Cards { get; set; } = null!;
        public DbSet<Company> Companies { get; set; } = null!;
        public DbSet<Activation> Activations { get; set; } = null!;

        public AppDBContext() : base()
        {
            //Database.EnsureDeleted();
            //Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            
            var config = new ConfigurationBuilder()
                        //.SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json")
                        .Build();

#if DEBUG1
            {
                optionsBuilder.UseNpgsql(config.GetConnectionString("DebugConnection"));
            }
#else
            {
            optionsBuilder.UseNpgsql(config.GetConnectionString("DefaultConnection"));
            }
#endif

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
