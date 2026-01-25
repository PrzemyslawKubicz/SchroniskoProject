using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Schronisko.Shared.Entities
{
    public class Animal
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Imię jest wymagane")]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Species { get; set; } = string.Empty; // np. Pies, Kot

        [Range(0, 30, ErrorMessage = "Wiek musi być między 0 a 30 lat")]
        public int Age { get; set; }

        public string? Description { get; set; }

        public string? ImageUrl { get; set; } // Do zdjęcia

        public string Status { get; set; } = "Do adopcji"; // Do adopcji, Oczekujący, Zaadoptowany

        public DateTime DateAdded { get; set; } = DateTime.Now;

        [NotMapped]
        public int DaysInShelter { get; set; }
    }
}