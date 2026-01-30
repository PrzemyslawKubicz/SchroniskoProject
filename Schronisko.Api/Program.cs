using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Schronisko.Api.Data;
using Swashbuckle.AspNetCore.Filters;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ==============================================================================
// 1. KONTENER SERWISÓW (Dependency Injection)
// Tutaj "uczymy" aplikacjê, z jakich narzêdzi ma korzystaæ.
// ==============================================================================

// KONTROLERY + JSON
// Dodajemy obs³ugê IgnoreCycles. Dlaczego?
// Jeœli Pies ma Wniosek, a Wniosek ma Psa, to przy wysy³aniu JSON robi siê nieskoñczona pêtla.
// Ta opcja mówi: "Jeœli ju¿ widzia³eœ ten obiekt, nie wpisuj go drugi raz, wstaw null/id".
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// BAZA DANYCH
// Rejestrujemy DataContext i mówimy mu, ¿eby ³¹czy³ siê z SQL Serverem
// u¿ywaj¹c adresu (ConnectionString) zapisanego w pliku appsettings.json.
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// SWAGGER (Dokumentacja API)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
    // Konfiguracja "K³ódki" w Swaggerze.
    // Dziêki temu mo¿esz wkleiæ Token JWT w przegl¹darce i testowaæ endpointy [Authorize].
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    options.OperationFilter<SecurityRequirementsOperationFilter>();
});

// UWIERZYTELNIANIE (JWT)
// Tutaj mówimy aplikacji: "Jak dostaniesz Token, to sprawdŸ czy pasuje do naszego tajnego klucza".
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true, // Sprawdzaj podpis (czy nikt nie podrobi³ tokena)
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
                .GetBytes(builder.Configuration.GetSection("AppSettings:Token").Value!)),
            ValidateIssuer = false, // Upraszczamy (nie sprawdzamy kto wyda³)
            ValidateAudience = false // Upraszczamy (nie sprawdzamy dla kogo)
        };
    });

// AUTORYZACJA
// Ustawiamy politykê "otwart¹" (FallbackPolicy = null).
// Oznacza to, ¿e domyœlnie endpointy s¹ publiczne, chyba ¿e damy nad nimi [Authorize].
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = null;
});

// CORS (Cross-Origin Resource Sharing)
// Pozwala przegl¹darce (Blazor Client na porcie np. 5000) 
// gadaæ z Serwerem (API na porcie np. 7000). Bez tego przegl¹darka zablokuje zapytania.
builder.Services.AddCors(options => {
    options.AddPolicy("AllowBlazorOrigin",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// ==============================================================================
// 2. PIPELINE 
// Tutaj decydujemy, co dzieje siê z ka¿dym zapytaniem przychodz¹cym do serwera.
// KOLEJNOŒÆ MA ZNACZENIE!
// ==============================================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowBlazorOrigin"); // Najpierw pozwalamy na po³¹czenie...

app.UseAuthentication(); // ...potem sprawdzamy KIM jesteœ...
app.UseAuthorization();  // ...a na koñcu CZY MO¯ESZ tu wejœæ.

app.MapControllers(); // Na samym koñcu kierujemy ruch do odpowiedniego Kontrolera.

// ==============================================================================
// 3. INICJALIZACJA BAZY DANYCH (Przy starcie)
// ==============================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<DataContext>();

        // WA¯NE: context.Database.Migrate()
        // To polecenie robi dwie rzeczy:
        // 1. Jeœli baza nie istnieje -> Tworzy j¹.
        // 2. Jeœli baza istnieje -> Aplikuje wszystkie zaleg³e migracje (Changes).
        // To w³aœnie dziêki temu funkcje SQL i Triggery trafiaj¹ do bazy!
        context.Database.Migrate();
        Console.WriteLine("--> Aktualizacja bazy danych (Migracje)...");

        // Seedowanie danych (Wype³nienie bazy na start)
        // Jeœli baza jest pusta, Seed.SeedData() wstawi Admina, Usera i Zwierzaki.
        Console.WriteLine("--> Seedowanie danych...");
        Seed.SeedData(context);

        Console.WriteLine("--> GOTOWE! Baza dzia³a.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"!!! B£¥D BAZY: {ex.Message}");
    }
}

app.Run(); // Start serwera