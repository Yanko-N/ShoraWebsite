using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ShoraWebsite.Models
{
    public class Perfil
    {
        [Key]
        public int Id { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string? UserId { get; set; }

        
        public IdentityUser? User {get; set;}

        public string Key { get; set; }

        public virtual ICollection<Reserva>? Reservas { get; set; } = new List<Reserva>();
    }
}
