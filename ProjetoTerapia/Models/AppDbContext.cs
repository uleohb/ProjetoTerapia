using Microsoft.EntityFrameworkCore;

namespace ProjetoTerapia.Models
{
    public class AppDbContext : DbContext
    {
        public DbSet<Clinica> Clinicas { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
    }
}