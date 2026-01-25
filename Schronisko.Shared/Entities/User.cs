using System.ComponentModel.DataAnnotations;

namespace Schronisko.Shared.Entities
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nazwa użytkownika jest wymagana")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-mail jest wymagany")]
        [EmailAddress(ErrorMessage = "Niepoprawny format e-maila")]
        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
    }
}