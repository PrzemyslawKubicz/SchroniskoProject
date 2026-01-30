using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Schronisko.Client.Auth
{
    // Ta klasa to "Strażnik" Twojej aplikacji w przeglądarce.
    // Dziedziczy po AuthenticationStateProvider, dzięki czemu Blazor wie, 
    // kogo ma uważać za zalogowanego.
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _js; // Pozwala wywoływać funkcje JavaScript (np. dostęp do sessionStorage)
        private readonly HttpClient _http; // Klient HTTP, którym strzelamy do API

        public CustomAuthStateProvider(IJSRuntime js, HttpClient http)
        {
            _js = js;
            _http = http;
        }

        // =================================================================
        // GŁÓWNA METODA: Sprawdza stan przy odświeżeniu strony
        // =================================================================
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // 1. Próbujemy wyciągnąć token z pamięci przeglądarki (Session Storage).
            // Używamy do tego JavaScriptu, bo C# (WebAssembly) nie ma bezpośredniego dostępu do dysku/pamięci przeglądarki.
            string token = await _js.InvokeAsync<string>("sessionStorage.getItem", "authToken");

            // Domyślnie zakładamy, że użytkownik jest NIEZALOGOWANY (pusta tożsamość)
            var identity = new ClaimsIdentity();
            _http.DefaultRequestHeaders.Authorization = null;

            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    // 2. Jeśli mamy token, musimy go "rozpakować" (Parsowanie JWT).
                    // Wyciągamy z niego Role i Nazwę użytkownika.
                    identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt", "name", "role");

                    // 3. WAŻNE: Wstrzykujemy ten token do każdego zapytania HTTP.
                    // Dzięki temu, jak za chwilę zawołasz api/animals, API zobaczy "Bearer eyJhbGci..." i Cię wpuści.
                    _http.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token.Replace("\"", ""));
                }
                catch
                {
                    // Jeśli token jest uszkodzony (np. ktoś ręcznie edytował go w konsoli przeglądarki),
                    // to usuwamy go i traktujemy użytkownika jako gościa.
                    await _js.InvokeVoidAsync("sessionStorage.removeItem", "authToken");
                    identity = new ClaimsIdentity();
                }
            }

            // Tworzymy obiekt użytkownika (ClaimsPrincipal) na podstawie danych z tokena (lub pusty).
            var user = new ClaimsPrincipal(identity);
            var state = new AuthenticationState(user);

            // Powiadamiamy komponenty Blazora (np. <AuthorizeView>), że stan się zmienił.
            NotifyAuthenticationStateChanged(Task.FromResult(state));

            return state;
        }

        // =================================================================
        // LOGOWANIE (Wywoływane po wpisaniu poprawnego hasła)
        // =================================================================
        public async Task Login(string token)
        {
            // Zapisujemy token w przeglądarce, żeby nie zginął po odświeżeniu strony (F5).
            await _js.InvokeVoidAsync("sessionStorage.setItem", "authToken", token);

            // Ponownie uruchamiamy sprawdzanie stanu, żeby odświeżyć widok (pokazać menu Admina).
            var authState = await GetAuthenticationStateAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(authState));
        }

        // =================================================================
        // WYLOGOWANIE
        // =================================================================
        public async Task Logout()
        {
            // Usuwamy token z pamięci.
            await _js.InvokeVoidAsync("sessionStorage.removeItem", "authToken");

            // Czyścimy nagłówek HTTP (żeby kolejne zapytania szły jako anonimowe).
            _http.DefaultRequestHeaders.Authorization = null;

            // Powiadamiamy system, że użytkownik jest teraz pusty (niezalogowany).
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
        }

        // =================================================================
        // PARSOWANIE TOKENA (Magia dekodowania Base64)
        // =================================================================
        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            // Token JWT składa się z 3 części oddzielonych kropkami. Środek (payload) to dane JSON zakodowane w Base64.
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            var claims = new List<Claim>();

            foreach (var kvp in keyValuePairs)
            {
                var key = kvp.Key;

                // === TŁUMACZ KLUCZY (Microsoft vs Standard) ===
                // API Microsoftu często zwraca role jako długi link XML: "http://schemas.microsoft.com/.../role"
                // Blazor natomiast szuka po prostu klucza "role".
                // Tutaj robimy "tłumaczenie", żeby <AuthorizeView Roles="Admin"> działało poprawnie.
                if (key == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                {
                    key = "role";
                }

                if (key == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")
                {
                    key = "name";
                }
                // ==============================================

                // Obsługa sytuacji, gdy ktoś ma wiele ról (tablica JSON)
                if (kvp.Value is JsonElement element && element.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in element.EnumerateArray())
                    {
                        claims.Add(new Claim(key, item.ToString()));
                    }
                }
                else
                {
                    claims.Add(new Claim(key, kvp.Value.ToString()));
                }
            }

            return claims;
        }

        // Helper do formatowania Base64 (dodaje brakujące znaki '=' na końcu, jeśli trzeba)
        private byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}