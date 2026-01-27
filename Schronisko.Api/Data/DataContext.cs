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
            // =========================================================
            // 1. KONFIGURACJA FUNKCJI I KOLUMN OBLICZANYCH
            // =========================================================
            modelBuilder.Entity<Animal>()
                .Property(a => a.DaysInShelter)
                .HasComputedColumnSql("dbo.fn_DaysInShelter(DateAdded)");

            // =========================================================
            // 2. KONFIGURACJA TRIGGERÓW
            // =========================================================
            modelBuilder.Entity<AdoptionRequest>()
                .ToTable(tb => tb.HasTrigger("trg_ApproveAdoption"));

            // =========================================================
            // 3. KONFIGURACJA RELACJI (Fluent API)
            // =========================================================
            // --- Relacja: Zwierzak (1) <-> Wnioski (N) ---
            modelBuilder.Entity<AdoptionRequest>()
                .HasOne(r => r.Animal)
                .WithMany()
                .HasForeignKey(r => r.AnimalId)
                .OnDelete(DeleteBehavior.SetNull);

            // --- Relacja: User (1) <-> Logi (N) ---
            modelBuilder.Entity<Log>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.SetNull); 
        }
    }
}