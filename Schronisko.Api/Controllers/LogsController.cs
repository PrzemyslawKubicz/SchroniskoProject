using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Schronisko.Api.Data;
using Schronisko.Shared.Entities;

namespace Schronisko.Api.Controllers
{
    // Kontroler "Audytu" - służy do podglądu historii zdarzeń w systemie.
    // Administrator może tu sprawdzić, kto usunął psa, kto się logował błędnie itp.
    [Route("api/[controller]")]
    [ApiController]
    // ZABEZPIECZENIE: To jest najważniejsza linijka!
    // [Authorize(Roles = "Admin")] na poziomie klasy oznacza, że
    // ŻADNA metoda w tym pliku nie zadziała dla zwykłego Usera ani Pracownika.
    // Tylko Administrator ma klucz do tych drzwi.
    [Authorize(Roles = "Admin")]
    public class LogsController : ControllerBase
    {
        private readonly DataContext _context;

        public LogsController(DataContext context)
        {
            _context = context;
        }

        // GET: api/logs
        [HttpGet]
        public async Task<ActionResult<List<Log>>> GetLogs()
        {
            // Budujemy zapytanie do bazy danych:
            return await _context.Logs
                // 1. .Include(l => l.User) -> Eager Loading (Ładowanie zachłanne)
                // W tabeli Logs mamy tylko "UserId" (np. 5).
                // Include każe bazie pobrać też dane tego Usera (np. "admin@schronisko.pl").
                // Bez tego na liście widziałbyś tylko numerki zamiast nazw użytkowników.
                .Include(l => l.User)

                // 2. Sortowanie: Najnowsze na górze.
                // Chcemy widzieć co stało się przed chwilą, a nie rok temu.
                .OrderByDescending(l => l.Timestamp)

                // 3. Optymalizacja wydajności (.Take(50))
                // Logów mogą być tysiące. Nie chcemy zapchać łącza wysyłaniem wszystkiego naraz.
                // Pobieramy "pacinę" ostatnich 50 zdarzeń.
                .Take(50)

                // 4. Wykonanie zapytania (dopiero tu SQL leci do bazy)
                .ToListAsync();
        }
    }
}