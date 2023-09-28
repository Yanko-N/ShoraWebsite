using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using shora.Models;
using ShoraWebsite.Models;

namespace ShoraWebsite.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Categoria>? Categoria { get; set; }
        public DbSet<Roupa>? Roupa { get; set; }
        public DbSet<Perfil>? Perfils { get; set; }
        public DbSet<Reserva>? Reserva { get; set; }
        public DbSet<Stock>? StockMaterial { get; set; }
    }
}