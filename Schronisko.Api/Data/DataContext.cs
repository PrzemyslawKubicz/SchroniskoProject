using Microsoft.EntityFrameworkCore;
using Schronisko.Shared.Entities;

namespace Schronisko.Api.Data
{
    // DataContext to "most" łączący kod C# z bazą danych SQL Server.
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        // =========================================================
        // REPREZENTACJA TABEL W BAZIE DANYCH
        // =========================================================
        public DbSet<Animal> Animals { get; set; }
        public DbSet<AdoptionRequest> AdoptionRequests { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Log> Logs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // =========================================================
            // 1. KONFIGURACJA ZWIERZĄT (FIX DLA EDYCJI)
            // =========================================================
            modelBuilder.Entity<Animal>(entity =>
            {
                // Poniższa linia oszukuje EF Core, mówiąc: "Ta tabela ma trigger".
                // To zmusza EF do użycia starszego, bezpiecznego sposobu zapisu (bez OUTPUT), co naprawia crash aplikacji.
                entity.ToTable(tb => tb.HasTrigger("trg_EF_Fix_ComputedColumn"));

                // Konfiguracja kolumny obliczanej.
                // Wartość nie jest liczona w C#, tylko pobierana z funkcji SQL 'dbo.fn_DaysInShelter'.
                entity.Property(a => a.DaysInShelter)
                      .HasComputedColumnSql("dbo.fn_DaysInShelter(DateAdded)");
            });

            // =========================================================
            // 2. KONFIGURACJA PRAWDZIWYCH TRIGGERÓW (WNIOSKI)
            // =========================================================
            // Tutaj informujemy EF, że tabela Wniosków ma faktyczny trigger w bazie.
            // Dzięki temu po zapisie (SaveChanges) EF wie, że musi odświeżyć dane,
            // bo trigger mógł zmienić coś w tle (np. statusy).
            modelBuilder.Entity<AdoptionRequest>()
                .ToTable(tb => tb.HasTrigger("trg_ApproveAdoption"));

            // =========================================================
            // 3. KONFIGURACJA RELACJI (ZACHOWANIE HISTORII)
            // =========================================================

            // --- Relacja: Zwierzak (1) <-> Wnioski (N) ---
            modelBuilder.Entity<AdoptionRequest>()
                .HasOne(r => r.Animal)
                .WithMany()
                .HasForeignKey(r => r.AnimalId)
                // [WAŻNE] DeleteBehavior.SetNull:
                // Jeśli usuniemy psa (np. pomyłkowo dodanego), to NIE usuwamy historii wniosków o niego.
                // Wnioski zostają w archiwum, a ich pole AnimalId zmienia się na NULL.
                .OnDelete(DeleteBehavior.SetNull);

            // --- Relacja: User (1) <-> Logi (N) ---
            modelBuilder.Entity<Log>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                // [WAŻNE] DeleteBehavior.SetNull:
                // Jeśli usuniemy konto pracownika, to NIE możemy stracić logów (historii operacji).
                // Logi zostają w systemie dla bezpieczeństwa, pole UserId zmienia się na NULL.
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}