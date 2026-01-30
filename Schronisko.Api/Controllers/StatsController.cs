using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Schronisko.Api.Data;

namespace Schronisko.Api.Controllers
{
    // Kontroler Statystyk - służy do karmienia "Dashboardu" (Panelu Głównego) Admina.
    // Wyświetla kafelki typu: "5 psów do adopcji", "2 nowe wnioski".
    [Route("api/[controller]")]
    [ApiController]
    public class StatsController : ControllerBase
    {
        private readonly DataContext _context;

        public StatsController(DataContext context)
        {
            _context = context;
        }

        // GET: api/stats
        // Zazwyczaj ten endpoint powinien być chroniony (dla Admina), 
        // ale jeśli chcesz pokazać statystyki na stronie głównej ("Uratowaliśmy już X zwierząt"), może być publiczny.
        [HttpGet]
        public async Task<ActionResult<ShelterStatsDto>> GetStats()
        {
            // 1. PODEJŚCIE HYBRYDOWE: Najpierw pytamy SQL Server
            // Używamy procedury składowanej 'sp_GetShelterStatistics', bo jest szybka 
            // i wykonuje obliczenia po stronie bazy danych.
            var query = _context.Database
                .SqlQuery<ShelterStatsDto>($"EXEC sp_GetShelterStatistics");

            // Materializujemy wynik (wykonujemy zapytanie)
            var result = await query.ToListAsync();

            // Pobieramy pierwszy wiersz wyników (lub tworzymy pusty obiekt, jeśli null)
            var stats = result.FirstOrDefault() ?? new ShelterStatsDto();

            // ================================================================
            // 2. SAFETY CHECK (Mechanizm Awaryjny)
            // ================================================================
            // Dlaczego to robimy? 
            // Czasami procedury SQL są wrażliwe na dokładne dopasowanie tekstu (np. literówki, spacje).
            // Jeśli procedura zwróci 0 (podejrzane!), a my wiemy, że dane mogą istnieć,
            // uruchamiamy "Plan B" - sprawdzamy to kodem C# (LINQ).

            // Sprawdzenie wniosków (Pending)
            if (stats.PendingRequests == 0)
            {
                // Używamy .Contains(), który jest "luźniejszy" niż SQL-owe "="
                // Złapie "Oczekujący", "Oczekujacy", "Pending" itp.
                stats.PendingRequests = await _context.AdoptionRequests
                    .CountAsync(r => r.Status.Contains("Oczekuj") || r.Status.Contains("Pending"));
            }

            // Sprawdzenie zwierząt (Available)
            if (stats.AvailableCount == 0)
            {
                // To samo dla zwierząt - jeśli SQL zwrócił 0, liczymy ręcznie w C#
                stats.AvailableCount = await _context.Animals
                    .CountAsync(a => a.Status.Contains("Do adopcji") || a.Status.Contains("Available"));
            }

            // Zwracamy gotowy obiekt z liczbami do wykresów/kafelków
            return Ok(stats);
        }
    }

    // DTO (Data Transfer Object)
    // Prosta klasa, która służy tylko do przewiezienia wyników z bazy do API.
    // WAŻNE: Nazwy właściwości (AvailableCount, AdoptedCount) muszą pasować do nazw kolumn
    // zwracanych przez procedurę SQL (SELECT COUNT(*) AS AvailableCount...).
    public class ShelterStatsDto
    {
        public int AvailableCount { get; set; }
        public int AdoptedCount { get; set; }
        public int PendingRequests { get; set; }
    }
}