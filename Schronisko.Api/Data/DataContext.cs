using Microsoft.EntityFrameworkCore;
using Schronisko.Shared.Entities;

namespace Schronisko.Api.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public DbSet<Animal> Animals { get; set; }
        public DbSet<AdoptionRequest> AdoptionRequests { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Log> Logs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Konfiguracja Triggera
            modelBuilder.Entity<AdoptionRequest>()
                .ToTable(tb => tb.HasTrigger("trg_ApproveAdoption"));

            // Konfiguracja relacji User <-> Wnioski
            // Jeden użytkownik może mieć wiele wniosków
            modelBuilder.Entity<AdoptionRequest>()
                .HasOne(r => r.User)       // Wniosek ma jednego autora
                .WithMany()           // User ma wiele wniosków (bez nawigacji w drugą stronę)
                .HasForeignKey(r => r.UserId); // Kluczem obcym jest UserId
        }
    }
}