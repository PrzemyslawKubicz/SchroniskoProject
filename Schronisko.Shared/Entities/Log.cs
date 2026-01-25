using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Schronisko.Shared.Entities
{
    public class Log
    {
        public int Id { get; set; }
        public int? UserId { get; set; } // Nullable, bo akcja może być anonimowa (np. próba logowania)
        [ForeignKey("UserId")]
        public User? User { get; set; }
        public string UserEmail { get; set; } = string.Empty; // Dla wygody, żeby nie robić JOIN
        public string Action { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}