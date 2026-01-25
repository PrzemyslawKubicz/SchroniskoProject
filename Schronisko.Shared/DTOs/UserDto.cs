namespace Schronisko.Shared.DTOs
{
    // Klasa do Rejestracji
    public class UserDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty; 
        public string Password { get; set; } = string.Empty;
    }

    // Klasa do Logowania 
    public class UserLoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}