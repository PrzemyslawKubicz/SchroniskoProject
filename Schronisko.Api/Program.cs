using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Schronisko.Api.Data;
using Swashbuckle.AspNetCore.Filters;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// === 1. KONTROLERY (TUTAJ BY£ BRAK) ===
// Dodajemy obs³ugê IgnoreCycles, ¿eby API nie d³awi³o siê relacjami w bazie
builder.Services.AddControllers().AddJsonOptions(options =>
{
    // To pozwala API wysy³aæ zagnie¿d¿one obiekty (Wniosek -> User)
    // i zapobiega b³êdom, jeœli obiekty wskazuj¹ na siebie nawzajem
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// 2. BAZA DANYCH
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. SWAGGER
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    options.OperationFilter<SecurityRequirementsOperationFilter>();
});

// 4. JWT (Uwierzytelnianie)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
                .GetBytes(builder.Configuration.GetSection("AppSettings:Token").Value!)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

// 5. AUTORYZACJA
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = null;
});

// 6. CORS
builder.Services.AddCors(options => {
    options.AddPolicy("AllowBlazorOrigin",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// === PIPELINE ===

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowBlazorOrigin");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// === SEKCJA STARTOWA BAZY DANYCH ===
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<DataContext>();

        // ZMIANA: Zamiast EnsureCreated, u¿ywamy Migrate().
        // To sprawia, ¿e EF Core uruchomi WSZYSTKIE Twoje pliki z folderu Migrations po kolei.
        // Dziêki temu trafi¹ do bazy te¿ Triggery i Procedury!
        context.Database.Migrate();
        Console.WriteLine("--> Aktualizacja bazy danych (Migracje)...");

        // Opcjonalnie: Usuñ bazê, jeœli chcesz zacz¹æ na czysto (odkomentuj poni¿sze 2 linie)
        //context.Database.EnsureDeleted();
        //Console.WriteLine("--> Stara baza usuniêta.");

        // Seedowanie danych (Admin, User, Zwierzêta)
        Console.WriteLine("--> Seedowanie danych...");
        Seed.SeedData(context);

        Console.WriteLine("--> GOTOWE! Baza dzia³a.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"!!! B£¥D BAZY: {ex.Message}");
    }
}

app.Run();