using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop; // <--- To jest kluczowe (Native JS)

namespace Schronisko.Client.Auth
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _js; // <--- Używamy natywnego Interopa
        private readonly HttpClient _http;

        public CustomAuthStateProvider(IJSRuntime js, HttpClient http)
        {
            _js = js;
            _http = http;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // 1. Pobieramy token używając czystego JavaScriptu
            // sessionStorage.getItem('authToken')
            string token = await _js.InvokeAsync<string>("sessionStorage.getItem", "authToken");

            var identity = new ClaimsIdentity();
            _http.DefaultRequestHeaders.Authorization = null;

            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    // Mówimy Blazorowi: szukaj nazwy pod kluczem "unique_name" (lub "name"), a roli pod "role"
                    identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt", "name", "role");
                    _http.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token.Replace("\"", ""));
                }
                catch
                {
                    // Token uszkodzony - usuwamy go (sessionStorage.removeItem)
                    await _js.InvokeVoidAsync("sessionStorage.removeItem", "authToken");
                    identity = new ClaimsIdentity();
                }
            }

            var user = new ClaimsPrincipal(identity);
            var state = new AuthenticationState(user);

            NotifyAuthenticationStateChanged(Task.FromResult(state));

            return state;
        }

        public async Task Login(string token)
        {
            // Zapisujemy token: sessionStorage.setItem('authToken', token)
            await _js.InvokeVoidAsync("sessionStorage.setItem", "authToken", token);

            var authState = await GetAuthenticationStateAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(authState));
        }

        public async Task Logout()
        {
            // Usuwamy token: sessionStorage.removeItem('authToken')
            await _js.InvokeVoidAsync("sessionStorage.removeItem", "authToken");

            _http.DefaultRequestHeaders.Authorization = null;
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
        }

        // --- Metody pomocnicze bez zmian ---
        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            var claims = new List<Claim>();

            foreach (var kvp in keyValuePairs)
            {
                // === TŁUMACZ KLUCZY (FIX) ===
                var key = kvp.Key;

                // Jeśli klucz to długi adres URL Microsoftu dla Roli -> zamień na "role"
                if (key == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                {
                    key = "role";
                }

                // Jeśli klucz to długi adres URL dla Nazwy -> zamień na "name"
                if (key == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")
                {
                    key = "name";
                }
                // ============================

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