using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace shora.Models
{
    public class Perfil
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public string? UserId { get; set; }

        
        public IdentityUser? User {get; set;}

        public virtual ICollection<Reserva>? Reservas { get; set; } = new List<Reserva>();
    }
}
