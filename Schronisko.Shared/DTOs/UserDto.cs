namespace Schronisko.Shared.DTOs
{
    // DTO = Data Transfer Object (Obiekt Transferu Danych).
    // Służą one wyłącznie do "transportu" danych między Klientem (przeglądarką) a Serwerem (API).
    // Znajdują się w projekcie "Shared", dzięki czemu oba projekty wiedzą, jak te dane wyglądają.

    // ==========================================
    // 1. DTO DO REJESTRACJI
    // ==========================================
    // Dlaczego nie używamy tu klasy "User"?
    // Bo klasa "User" w bazie ma pola takie jak Id, PasswordHash, Role, DateCreated itp.
    // Przy rejestracji użytkownik podaje tylko te trzy rzeczy. Resztę ustawia serwer.
    public class UserDto
    {
        public string Username { get; set; } = string.Empty; // Inicjalizacja pustym stringiem, żeby uniknąć nulli
        public string Email { get; set; } = string.Empty;

        // To jest "czyste" hasło (np. "mojeHaslo123").
        // Przesyłamy je HTTPS-em do serwera.
        // Serwer zamieni je na Hash i dopiero wtedy zapisze w bazie.
        // Nigdy nie zapisujemy tego pola w bazie danych!
        public string Password { get; set; } = string.Empty;
    }

    // ==========================================
    // 2. DTO DO LOGOWANIA
    // ==========================================
    // Tutaj potrzebujemy jeszcze mniej danych - tylko login i hasło.
    // Nie interesuje nas email przy samym logowaniu (chyba że logujemy emailem).
    public class UserLoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}