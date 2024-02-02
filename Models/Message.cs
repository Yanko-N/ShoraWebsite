using Microsoft.AspNetCore.Identity;

namespace ShoraWebsite.Models
{
    public class Message
    {
        public int Id { get; set; }

        public string? UserId { get; set; }
        public IdentityUser? User { get; set; }


        public int ReservaId { get; set; }

        public Reserva? Reserva { get; set; }

        public string Text { get; set; }
        public DateTime Timestamp { get; set; }

        public bool IsAdmin { get; set; } = false;

        public string IV { get; set; }
    }
}
