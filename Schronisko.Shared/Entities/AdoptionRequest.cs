using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Ważne dla ForeignKey

namespace Schronisko.Shared.Entities
{
    public class AdoptionRequest
    {
        public int Id { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.Now;
        public DateTime? DecisionDate { get; set; }

        public string Status { get; set; } = "Oczekujący";
        public string Reason { get; set; } = string.Empty;

        // --- RELACJA ZE ZWIERZĘCIEM ---
        public int? AnimalId { get; set; }
        public Animal? Animal { get; set; }

        // --- RELACJA Z UŻYTKOWNIKIEM ---
        public int UserId { get; set; } // Klucz obcy
        public User? User { get; set; } // Obiekt, po którym sięgamy po Username
    }
}