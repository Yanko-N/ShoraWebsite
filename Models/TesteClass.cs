using System.ComponentModel.DataAnnotations;
using System.Security.Policy;

namespace ShoraWebsite.Models
{
    public class TesteClass
    {
        [Key]
        public int Id { get; set; }

        public HashSet<string> Files { get; set; }
    }
}
