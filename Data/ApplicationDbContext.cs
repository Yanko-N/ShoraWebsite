using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using shora.Models;

namespace ShoraWebsite.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<shora.Models.Categoria>? Categoria { get; set; }
        public DbSet<shora.Models.Roupa>? Roupa { get; set; }
    }
}