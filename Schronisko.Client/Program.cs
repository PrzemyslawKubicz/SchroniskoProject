using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Schronisko.Client;
using Schronisko.Client.Auth;
using Microsoft.AspNetCore.Components.Authorization;

// Tworzymy "Budowniczego" aplikacji WebAssembly.
// To on przygotuje œrodowisko uruchomieniowe w przegl¹darce.
var builder = WebAssemblyHostBuilder.CreateDefault(args);

// 1. MONTOWANIE APLIKACJI
// Mówimy Blazorowi: "WeŸ komponent App.razor i wstaw go do pliku index.html
// w miejsce diva o id='app'". To tutaj C# przejmuje kontrolê nad HTMLem.
builder.RootComponents.Add<App>("#app");

// Obs³uga tagów w sekcji <head> (np. dynamiczna zmiana tytu³u strony <title>).
builder.RootComponents.Add<HeadOutlet>("head::after");

// 2. KONFIGURACJA ADRESU API
// Tutaj wpisujemy adres, pod którym dzia³a Twój projekt Server (Swagger).
// WA¯NE: Jeœli zmienisz port w launchSettings.json serwera, musisz zmieniæ go te¿ tutaj!
string apiAddress = "https://localhost:7010";

// Rejestracja HttpClienta w kontenerze DI (Dependency Injection).
// Dziêki temu, w ka¿dym pliku .razor mo¿esz napisaæ "@inject HttpClient Http"
// i dostaniesz gotowego klienta ustawionego na adres Twojego API.
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiAddress) });

// =============================================================
// 3. KONFIGURACJA AUTORYZACJI (BEZPIECZEÑSTWA)
// =============================================================

// W³¹czamy mechanizmy autoryzacji Blazora ("Core", bo to wersja dla przegl¹darki).
// Bez tego <AuthorizeView> i [Authorize] nie zadzia³aj¹.
builder.Services.AddAuthorizationCore();

// KLUCZOWY MOMENT: Rejestracja Twojego CustomAuthStateProvider.
// Mówimy systemowi:
// "Kiedykolwiek jakiœ komponent zapyta o AuthenticationStateProvider (stan logowania),
// u¿yj mojej klasy CustomAuthStateProvider."
// To w³aœnie to sprawia, ¿e logowanie z sessionStorage ³¹czy siê z widokami Blazora.
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

// Budujemy i uruchamiamy aplikacjê w przegl¹darce u¿ytkownika.
await builder.Build().RunAsync();