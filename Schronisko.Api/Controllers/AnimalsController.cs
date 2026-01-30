using System.Security.Claims; // Potrzebne do odczytania ID z tokena
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Schronisko.Api.Data;
using Schronisko.Shared.Entities;

namespace Schronisko.Api.Controllers
{
    // Kontroler API odpowiedzialny za zarządzanie kartoteką zwierząt.
    // Tutaj trafiają zapytania z przeglądarki (Frontend) do Bazy Danych.
    [Route("api/[controller]")]
    [ApiController]
    public class AnimalsController : ControllerBase
    {
        private readonly DataContext _context;

        // Konstruktor z wstrzykiwaniem zależności (Dependency Injection)
        // Dzięki temu kontroler ma dostęp do bazy danych.
        public AnimalsController(DataContext context)
        {
            _context = context;
        }

        // --- METODA POMOCNICZA ---
        // Służy do wyciągania ID zalogowanego użytkownika z Tokena JWT (tego, który przyszedł w nagłówku).
        // Potrzebujemy tego ID, żeby zapisać w logach, KTO dodał lub usunął zwierzaka.
        private int? GetCurrentUserId()
        {
            // Szukamy w "paszporcie" użytkownika (Claims) wpisu o nazwie NameIdentifier (to zazwyczaj ID)
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (idClaim != null && int.TryParse(idClaim.Value, out int userId))
            {
                return userId;
            }
            return null; // Zwraca null, jeśli użytkownik nie jest zalogowany lub token jest błędny
        }

        // ==========================================
        // 1. GET: api/animals (Pobierz wszystkie)
        // ==========================================
        // To jest metoda publiczna - każdy (nawet niezalogowany) może zobaczyć listę zwierząt.
        // Dlatego używamy [AllowAnonymous].
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<Animal>>> GetAllAnimals()
        {
            // Pobiera wszystkie rekordy z tabeli Animals i zamienia je na listę JSON
            return await _context.Animals.ToListAsync();
        }

        // ==========================================
        // 2. GET: api/animals/{id} (Pobierz jednego)
        // ==========================================
        // Również publiczne - szczegóły konkretnego zwierzaka.
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Animal>> GetAnimal(int id)
        {
            var animal = await _context.Animals.FindAsync(id);

            if (animal == null)
                return NotFound("Nie znaleziono zwierzaka :("); // Zwraca błąd 404

            return Ok(animal); // Zwraca kod 200 i obiekt zwierzaka
        }

        // ==========================================
        // 3. POST: api/animals (Dodaj nowego)
        // ==========================================
        // ZABEZPIECZENIE: Tylko Admin i Pracownik mogą dodawać zwierzęta.
        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<ActionResult<Animal>> AddAnimal(Animal animal)
        {
            // 1. Dodajemy zwierzaka do kontekstu EF (jeszcze nie do bazy)
            _context.Animals.Add(animal);

            // 2. PRZYGOTOWANIE LOGÓW (Audit Log)
            // Pobieramy dane osoby, która wykonuje tę akcję
            var userId = GetCurrentUserId();
            var userName = User.Identity?.Name ?? "Nieznany";

            // 3. Tworzymy wpis w tabeli Logs
            _context.Logs.Add(new Log
            {
                UserId = userId,
                UserEmail = userName,
                Action = $"Dodano nowego zwierzaka: {animal.Name} ({animal.Species})",
                Timestamp = DateTime.Now
            });

            // 4. Zapisujemy WSZYSTKO (zwierzaka i log) w jednej transakcji do bazy
            await _context.SaveChangesAsync();

            return Ok(animal);
        }

        // ==========================================
        // 4. PUT: api/animals/{id} (Edytuj)
        // ==========================================
        // Edycja danych (np. zmiana opisu, wieku). Dostępna dla personelu.
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> UpdateAnimal(int id, Animal animal)
        {
            // Zabezpieczenie: sprawdzamy czy ID w adresie URL zgadza się z ID w przesyłanym obiekcie
            if (id != animal.Id)
            {
                return BadRequest("ID w URL nie zgadza się z ID obiektu.");
            }

            // Informujemy EF, że ten obiekt został zmieniony
            _context.Entry(animal).State = EntityState.Modified;

            // LOGOWANIE EDYCJI
            var userId = GetCurrentUserId();
            var userName = User.Identity?.Name ?? "Nieznany";

            _context.Logs.Add(new Log
            {
                UserId = userId,
                UserEmail = userName,
                Action = $"Edytowano dane zwierzaka ID: {id} ({animal.Name})",
                Timestamp = DateTime.Now
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Obsługa sytuacji, gdy ktoś inny usunął zwierzaka w trakcie naszej edycji
                if (!await _context.Animals.AnyAsync(a => a.Id == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent(); // 204 No Content - standardowa odpowiedź przy udanej edycji
        }

        // ==========================================
        // 5. DELETE: api/animals/{id} (Usuń)
        // ==========================================
        // ZABEZPIECZENIE: Tylko Admin (najwyższa rola) może usuwać rekordy!
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteAnimal(int id)
        {
            var animal = await _context.Animals.FindAsync(id);
            if (animal == null)
                return NotFound();

            // Oznaczamy do usunięcia
            _context.Animals.Remove(animal);

            // LOGOWANIE USUNIĘCIA
            var userId = GetCurrentUserId();
            var userName = User.Identity?.Name ?? "Nieznany";

            _context.Logs.Add(new Log
            {
                UserId = userId,
                UserEmail = userName,
                Action = $"Usunięto zwierzaka ID: {id} ({animal?.Name})",
                Timestamp = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return Ok("Zwierzak usunięty.");
        }
    }
}