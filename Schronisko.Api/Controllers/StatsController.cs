using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Schronisko.Api.Data;

namespace Schronisko.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatsController : ControllerBase
    {
        private readonly DataContext _context;

        public StatsController(DataContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<ShelterStatsDto>> GetStats()
        {
            // 1. Pobieramy dane z Twojej procedury SQL
            var query = _context.Database
                .SqlQuery<ShelterStatsDto>($"EXEC sp_GetShelterStatistics");

            // Wykonujemy zapytanie asynchronicznie
            var result = await query.ToListAsync();
            var stats = result.FirstOrDefault();

            // 2. SAFETY CHECK (Obejście problemu 0):
            // Jeśli procedura z jakiegoś powodu zwróciła 0, a wiemy, że mogą być wnioski,
            // sprawdzamy to ręcznie w C# używając Contains (jest odporniejsze na spacje).

            if (stats.PendingRequests == 0)
            {
                stats.PendingRequests = await _context.AdoptionRequests
                    .CountAsync(r => r.Status.Contains("Oczekuj") || r.Status.Contains("Pending"));
            }

            // 3. To samo dla zwierząt - dla pewności
            if (stats.AvailableCount == 0)
            {
                stats.AvailableCount = await _context.Animals
                    .CountAsync(a => a.Status.Contains("Do adopcji") || a.Status.Contains("Available"));
            }

            return Ok(stats);
        }
    }

    public class ShelterStatsDto
    {
        // Te nazwy muszą pasować do kolumn w procedurze SQL (SELECT ... as AvailableCount)
        public int AvailableCount { get; set; }
        public int AdoptedCount { get; set; }
        public int PendingRequests { get; set; }
    }
}