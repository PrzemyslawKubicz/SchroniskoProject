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
    // Kontroler odpowiedzialny za uwierzytelnianie (Logowanie i Rejestrację).
    // To tutaj generujemy tokeny JWT, które są "przepustką" do reszty systemu.
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;

        // Wstrzykujemy DataContext (baza) i IConfiguration (żeby odczytać tajny klucz z appsettings.json)
        public AuthController(DataContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // ==========================================
        // REJESTRACJA Z E-MAILEM
        // ==========================================
        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(UserDto request)
        {
            // WALIDACJA: Sprawdzamy czy taki user lub email już istnieje w bazie.
            // Zapobiega to błędom duplikatów i bałaganowi w danych.
            if (await _context.Users.AnyAsync(u => u.Username == request.Username || u.Email == request.Email))
            {
                return BadRequest("Użytkownik o takiej nazwie lub e-mailu już istnieje.");
            }

            // BEZPIECZEŃSTWO: Nigdy nie zapisujemy hasła otwartym tekstem!
            // Używamy BCrypt do stworzenia "hasha". Nawet jak haker wykradnie bazę, nie odczyta haseł.
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash, // Zapisujemy zaszyfrowane hasło
                Role = "User" // Domyślna rola to zwykły użytkownik
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(user);
        }

        // ==========================================
        // LOGOWANIE (LOGIN LUB EMAIL)
        // ==========================================
        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(UserLoginDto request)
        {
            // Szukamy użytkownika po Loginie LUB po Emailu (wygoda dla usera)
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Username || u.Username == request.Username);

            // --- 1. SCENARIUSZ: Użytkownik nie istnieje ---
            if (user == null)
            {
                // AUDYT BEZPIECZEŃSTWA:
                // Logujemy, że ktoś próbuje zgadnąć login. Jeśli takich logów będzie 1000 na minutę, to znaczy, że ktoś nas atakuje.
                _context.Logs.Add(new Log
                {
                    UserId = null, // Nie znamy ID, bo user nie istnieje
                    UserEmail = request.Username, // Ale wiemy, co wpisał atakujący
                    Action = "Nieudana próba logowania: Użytkownik nie istnieje",
                    Timestamp = DateTime.Now
                });
                await _context.SaveChangesAsync();

                return BadRequest("Użytkownik nie znaleziony.");
            }

            // --- 2. SCENARIUSZ: Błędne hasło ---
            // Porównujemy hasło wpisane (request.Password) z hashem w bazie (user.PasswordHash)
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                // AUDYT: Logujemy błędne hasło. Ważne, żeby wiedzieć, czy legalny użytkownik po prostu się pomylił, czy ktoś łamie mu hasło.
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

            // --- 3. SCENARIUSZ: Sukces (Login i Hasło OK) ---

            // Generujemy cyfrową przepustkę (Token JWT)
            string token = CreateToken(user);

            // Logujemy sukces - wiemy kiedy pracownicy/użytkownicy wchodzą do systemu
            _context.Logs.Add(new Log
            {
                UserId = user.Id,
                UserEmail = user.Email,
                Action = "Zalogowano pomyślnie",
                Timestamp = DateTime.Now
            });
            await _context.SaveChangesAsync();

            return Ok(token); // Wysyłamy token do przeglądarki
        }

        // ==========================================
        // METODA POMOCNICZA: TWORZENIE TOKENA JWT
        // ==========================================
        private string CreateToken(User user)
        {
            // 1. Claims (Roszczenia) - to informacje zaszyte w środku tokena.
            // Dzięki temu serwer nie musi za każdym razem pytać bazy "kim on jest?", bo ma to w tokenie.
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // ID
                new Claim(ClaimTypes.Name, user.Username), // Login
                new Claim(ClaimTypes.Role, user.Role) // Rola (Admin/User) - kluczowe dla [Authorize]
            };

            // 2. Pobieramy tajny klucz z appsettings.json. Tylko serwer go zna.
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration.GetSection("AppSettings:Token").Value!));

            // 3. Podpisujemy token tym kluczem (jak pieczęć notariusza).
            // Jeśli ktoś spróbuje zmienić "User" na "Admin" w tokenie, podpis się nie zgodzi i serwer to odrzuci.
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1), // Token ważny przez 1 dzień
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }
    }
}