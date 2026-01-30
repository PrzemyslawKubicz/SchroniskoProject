using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Schronisko.Api.Data;
using Schronisko.Shared.Entities;

namespace Schronisko.Api.Controllers
{
    // Kontroler zarządzający całym procesem adopcyjnym:
    // Składanie wniosków, przeglądanie ich przez personel oraz podejmowanie decyzji.
    [Route("api/[controller]")]
    [ApiController]
    public class AdoptionRequestsController : ControllerBase
    {
        private readonly DataContext _context;

        // Wstrzykiwanie zależności bazy danych (Dependency Injection)
        public AdoptionRequestsController(DataContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. GET: Pobierz wszystkie wnioski (Dla Personelu)
        // ==========================================
        // ZABEZPIECZENIE: Te dane są poufne (dane osobowe użytkowników), 
        // dlatego dostęp mają TYLKO Administratorzy i Pracownicy.
        [HttpGet]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<ActionResult<List<AdoptionRequest>>> GetAdoptionRequests()
        {
            return await _context.AdoptionRequests
                // .Include() to tzw. Eager Loading. 
                // Mówimy bazie: "Jak pobierasz wniosek, to od razu dobierz dane Zwierzaka i Użytkownika".
                // Bez tego pola Animal i User byłyby null.
                .Include(r => r.Animal)
                .Include(r => r.User)
                // Sortujemy od najnowszych, żeby personel widział świeże zgłoszenia na górze.
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();
        }

        // ==========================================
        // 2. POST: Złóż wniosek (Dla Zwykłego Użytkownika)
        // ==========================================
        [HttpPost]
        [Authorize(Roles = "User")] // Tylko zalogowany użytkownik może złożyć wniosek
        public async Task<ActionResult<AdoptionRequest>> CreateRequest(AdoptionRequest request)
        {
            // BEZPIECZEŃSTWO: Nie wierzymy danym przesłanym w JSON (request.UserId).
            // ID użytkownika wyciągamy "siłowo" z bezpiecznego Tokena JWT, który przysłała przeglądarka.
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity?.Name ?? "Nieznany"; // Potrzebne do historii (logów)

            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized("Błąd tożsamości. Zaloguj się ponownie.");
            }

            // Nadpisujemy dane systemowe:
            request.UserId = int.Parse(userIdString); // Przypisujemy wniosek do osoby, która faktycznie wysłała żądanie
            request.RequestDate = DateTime.Now;       // Data złożenia to "teraz"
            request.Status = "Oczekujący";            // Domyślny status na start

            _context.AdoptionRequests.Add(request);

            // LOGOWANIE AUDYTOWE (Audit Log):
            // Tworzymy wpis w tabeli Logs, żeby administrator wiedział, co dzieje się w systemie.
            _context.Logs.Add(new Log
            {
                UserId = request.UserId, // Relacja do Usera składającego wniosek
                UserEmail = userName,
                Action = $"Złożono nowy wniosek adopcyjny o zwierzę ID: {request.AnimalId}",
                Timestamp = DateTime.Now
            });

            await _context.SaveChangesAsync(); // Zapisujemy obie rzeczy (wniosek + log) w jednej transakcji

            return Ok(request);
        }

        // ==========================================
        // 3. PUT: Zmień status wniosku (Decyzja Admina/Pracownika)
        // ==========================================
        // To jest najważniejsza metoda biznesowa - tu zapada decyzja o adopcji.
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Employee")] // Zwykły user nie może sam sobie zatwierdzić wniosku!
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            // Pobieramy dane osoby decyzyjnej z tokena (kto kliknął przycisk?)
            var staffIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var staffName = User.Identity?.Name ?? "Personel";
            int? staffId = string.IsNullOrEmpty(staffIdString) ? null : int.Parse(staffIdString);

            // Pobieramy wniosek z bazy wraz z danymi zwierzaka (potrzebne do zmiany jego statusu)
            var request = await _context.AdoptionRequests
                .Include(r => r.Animal)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound("Nie znaleziono wniosku.");

            string oldStatus = request.Status;

            // Aktualizacja statusu wniosku
            request.Status = status;
            request.DecisionDate = DateTime.Now;

            // LOGIKA BIZNESOWA (Cofanie rezerwacji):
            // Jeśli pracownik odrzuca wniosek, a zwierzak był np. wstępnie zablokowany/zaadoptowany,
            // to musimy go "uwolnić", żeby znów był widoczny jako "Do adopcji" dla innych ludzi.
            if (status == "Odrzucony" && request.Animal != null && request.Animal.Status == "Zaadoptowany")
            {
                request.Animal.Status = "Do adopcji";
            }
            // UWAGA: Logika "Zatwierdzania" (zmiana statusu psa na "Zaadoptowany") 
            // dzieje się prawdopodobnie w bazie danych (Trigger) lub powinna być tutaj dopisana,
            // jeśli nie używasz Triggerów SQL.

            // LOGOWANIE DECYZJI:
            // Zapisujemy, kto (staffId) zmienił status i z jakiego na jaki.
            _context.Logs.Add(new Log
            {
                UserId = staffId,
                UserEmail = staffName,
                Action = $"Decyzja wniosek #{id}: '{oldStatus}' -> '{status}'",
                Timestamp = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Ok(request);
        }

        // ==========================================
        // 4. GET: Pobierz TYLKO moje wnioski (Panel Użytkownika)
        // ==========================================
        [HttpGet("my")]
        [Authorize] // Dostępne dla każdego zalogowanego
        public async Task<ActionResult<List<AdoptionRequest>>> GetMyRequests()
        {
            // Identyfikacja użytkownika z Tokena
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            int userId = int.Parse(userIdString);

            // Pobieramy wnioski i filtrujemy: Where(r => r.UserId == userId)
            // To gwarantuje, że Jan Kowalski widzi tylko wnioski Jana Kowalskiego.
            return await _context.AdoptionRequests
                .Include(r => r.Animal) // Dołączamy zdjęcia/imiona zwierząt, żeby ładnie wyświetlić je na liście
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();
        }
    }
}