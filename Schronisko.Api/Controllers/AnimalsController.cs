using System.Security.Claims; // Potrzebne do odczytania ID z tokena
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Schronisko.Api.Data;
using Schronisko.Shared.Entities;

namespace Schronisko.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnimalsController : ControllerBase
    {
        private readonly DataContext _context;

        public AnimalsController(DataContext context)
        {
            _context = context;
        }

        // --- METODA POMOCNICZA ---
        // Wyciąga ID zalogowanego użytkownika z Tokena JWT
        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (idClaim != null && int.TryParse(idClaim.Value, out int userId))
            {
                return userId;
            }
            return null; // Jeśli coś pójdzie nie tak (np. brak tokena)
        }

        // 1. GET: api/animals (Pobierz wszystkie)
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<Animal>>> GetAllAnimals()
        {
            return await _context.Animals.ToListAsync();
        }

        // 2. GET: api/animals/{id} (Pobierz jednego)
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Animal>> GetAnimal(int id)
        {
            var animal = await _context.Animals.FindAsync(id);
            if (animal == null)
                return NotFound("Nie znaleziono zwierzaka :(");

            return Ok(animal);
        }

        // 3. POST: api/animals (Dodaj nowego)
        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<ActionResult<Animal>> AddAnimal(Animal animal)
        {
            _context.Animals.Add(animal);

            // LOGOWANIE Z RELACJĄ
            var userId = GetCurrentUserId();
            var userName = User.Identity?.Name ?? "Nieznany";

            _context.Logs.Add(new Log
            {
                UserId = userId,
                UserEmail = userName,
                Action = $"Dodano nowego zwierzaka: {animal.Name} ({animal.Species})",
                Timestamp = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Ok(animal);
        }

        // 4. PUT: api/animals/{id} (Edytuj)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> UpdateAnimal(int id, Animal animal)
        {
            if (id != animal.Id)
            {
                return BadRequest("ID w URL nie zgadza się z ID obiektu.");
            }

            _context.Entry(animal).State = EntityState.Modified;

            // LOGOWANIE EDYCJI
            var userId = GetCurrentUserId();
            var userName = User.Identity?.Name ?? "Nieznany";

            _context.Logs.Add(new Log
            {
                UserId = userId, // Relacja
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
                if (!await _context.Animals.AnyAsync(a => a.Id == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // 5. DELETE: api/animals/{id} (Usuń)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Tylko Admin może usuwać!
        public async Task<ActionResult> DeleteAnimal(int id)
        {
            var animal = await _context.Animals.FindAsync(id);
            if (animal == null)
                return NotFound();

            _context.Animals.Remove(animal);

            // LOGOWANIE USUNIĘCIA
            var userId = GetCurrentUserId();
            var userName = User.Identity?.Name ?? "Nieznany";

            _context.Logs.Add(new Log
            {
                UserId = userId, // Relacja
                UserEmail = userName,
                Action = $"Usunięto zwierzaka ID: {id} ({animal?.Name})",
                Timestamp = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return Ok("Zwierzak usunięty.");
        }
    }
}