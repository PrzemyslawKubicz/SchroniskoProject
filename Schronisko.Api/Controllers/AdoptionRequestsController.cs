using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Schronisko.Api.Data;
using Schronisko.Shared.Entities;

namespace Schronisko.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdoptionRequestsController : ControllerBase
    {
        private readonly DataContext _context;

        public AdoptionRequestsController(DataContext context)
        {
            _context = context;
        }

        // 1. GET: Pobierz wszystkie wnioski
        // ZABEZPIECZENIE: Tylko Admin i Pracownik mogą widzieć dane osobowe!
        [HttpGet]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<ActionResult<List<AdoptionRequest>>> GetAdoptionRequests()
        {
            return await _context.AdoptionRequests
                .Include(r => r.Animal)
                .Include(r => r.User)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();
        }

        // 2. POST: Złóż wniosek (User)
        [HttpPost]
        [Authorize(Roles = "User")] // Każdy zalogowany
        public async Task<ActionResult<AdoptionRequest>> CreateRequest(AdoptionRequest request)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity?.Name ?? "Nieznany"; // Do logów

            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized("Błąd tożsamości. Zaloguj się ponownie.");
            }

            request.UserId = int.Parse(userIdString);
            request.RequestDate = DateTime.Now;
            request.Status = "Oczekujący";

            _context.AdoptionRequests.Add(request);

            // DODATKOWO: Logujemy, że ktoś złożył wniosek (dla śladu w systemie)
            _context.Logs.Add(new Log
            {
                UserId = request.UserId, // Relacja do Usera składającego wniosek
                UserEmail = userName,
                Action = $"Złożono nowy wniosek adopcyjny o zwierzę ID: {request.AnimalId}",
                Timestamp = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Ok(request);
        }

        // 3. PUT: Zmień status wniosku + EFEKT DOMINA
        // ZABEZPIECZENIE: Tylko personel może decydować
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            // Pobieramy dane osoby decyzyjnej (Admina/Pracownika)
            var staffIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var staffName = User.Identity?.Name ?? "Personel";
            int? staffId = string.IsNullOrEmpty(staffIdString) ? null : int.Parse(staffIdString);

            var request = await _context.AdoptionRequests
                .Include(r => r.Animal)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound("Nie znaleziono wniosku.");

            string oldStatus = request.Status;
            request.Status = status;
            request.DecisionDate = DateTime.Now;

            // Trigger obsługuje zatwierdzanie adopcji 
                        
            if (status == "Odrzucony" && request.Animal != null && request.Animal.Status == "Zaadoptowany")
            {
                request.Animal.Status = "Do adopcji";
            }

            // LOGOWANIE DECYZJI
            _context.Logs.Add(new Log
            {
                UserId = staffId, // Kto podjął decyzję (Admin/Pracownik)
                UserEmail = staffName,
                Action = $"Decyzja wniosek #{id}: '{oldStatus}' -> '{status}'",
                Timestamp = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Ok(request);
        }

        // 4. GET: Pobierz TYLKO moje wnioski
        [HttpGet("my")]
        [Authorize] // Każdy zalogowany widzi swoje
        public async Task<ActionResult<List<AdoptionRequest>>> GetMyRequests()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            int userId = int.Parse(userIdString);

            return await _context.AdoptionRequests
                .Include(r => r.Animal)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();
        }
    }
}