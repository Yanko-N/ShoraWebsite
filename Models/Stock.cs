using System.ComponentModel.DataAnnotations;

namespace ShoraWebsite.Models
{
    public class Stock
    {
        [Key]
        public int Id { get; set; }

        public int RoupaId { get; set; }

        public Roupa? Roupa { get; set; }

        public string Tamanho { get; set; }

        public int Quantidade { get; set; }
    }
}
