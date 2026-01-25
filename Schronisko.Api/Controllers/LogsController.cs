using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Schronisko.Api.Data;
using Schronisko.Shared.Entities;

namespace Schronisko.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class LogsController : ControllerBase
    {
        private readonly DataContext _context;

        public LogsController(DataContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<Log>>> GetLogs()
        {
            // Pobieramy logi
            // .Include(l => l.User) -> TO JEST KLUCZOWE! Pobiera dane usera po relacji.
            // .Take(50) -> Pobieramy tylko 50 ostatnich, żeby było czytelnie.

            return await _context.Logs
                .Include(l => l.User)
                .OrderByDescending(l => l.Timestamp)
                .Take(50)
                .ToListAsync();
        }
    }
}