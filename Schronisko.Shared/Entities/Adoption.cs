using System.ComponentModel.DataAnnotations;

namespace Schronisko.Shared.Entities
{
    public class Adoption
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int AnimalId { get; set; }
        public DateTime AdoptionDate { get; set; } = DateTime.Now;
    }
}