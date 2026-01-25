using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Schronisko.Api.Data;
using Schronisko.Shared.DTOs;
using Schronisko.Shared.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Schronisko.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(DataContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // REJESTRACJA Z E-MAILEM
        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(UserDto request)
        {
            // Sprawdzamy czy taki user lub email już istnieje
            if (await _context.Users.AnyAsync(u => u.Username == request.Username || u.Email == request.Email))
            {
                return BadRequest("Użytkownik o takiej nazwie lub e-mailu już istnieje.");
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Username = request.Username,
                Email = request.Email, 
                PasswordHash = passwordHash,
                Role = "User"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(user);
        }

        // LOGOWANIE (LOGIN LUB EMAIL)
        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(UserLoginDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Username || u.Username == request.Username);

            // 1. SCENARIUSZ: Użytkownik nie istnieje
            if (user == null)
            {
                // Logujemy próbę włamania / błędny login
                _context.Logs.Add(new Log
                {
                    UserId = null, // Nie znamy ID, bo user nie istnieje
                    UserEmail = request.Username, // Ale wiemy, co wpisał
                    Action = "Nieudana próba logowania: Użytkownik nie istnieje",
                    Timestamp = DateTime.Now
                });
                await _context.SaveChangesAsync();

                return BadRequest("Użytkownik nie znaleziony.");
            }

            // 2. SCENARIUSZ: Błędne hasło
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                // Logujemy błędne hasło (ważne dla bezpieczeństwa!)
                _context.Logs.Add(new Log
                {
                    UserId = user.Id,
                    UserEmail = user.Email,
                    Action = "Nieudana próba logowania: Błędne hasło",
                    Timestamp = DateTime.Now
                });
                await _context.SaveChangesAsync();

                return BadRequest("Błędne hasło.");
            }

            // 3. SCENARIUSZ: Sukces
            string token = CreateToken(user);

            // Logujemy sukces
            _context.Logs.Add(new Log
            {
                UserId = user.Id,
                UserEmail = user.Email,
                Action = "Zalogowano pomyślnie",
                Timestamp = DateTime.Now
            });
            await _context.SaveChangesAsync();

            return Ok(token);
        }

        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration.GetSection("AppSettings:Token").Value!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }
    }
}