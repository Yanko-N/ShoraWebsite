using Microsoft.AspNetCore.Identity;
using shora.Models;
using System.ComponentModel.DataAnnotations;

namespace ShoraWebsite.Models
{
    public class Chat
    {
        [Key]
        public int Id { get; set; }

        public List<Message>? MessageHistory { get; set; }

        public int ReservaId { get; set; }

        public Reserva? Reserva { get; set; }


    }
}
