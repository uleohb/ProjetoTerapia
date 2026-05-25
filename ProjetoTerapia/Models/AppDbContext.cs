using Microsoft.EntityFrameworkCore;

namespace ProjetoTerapia.Models
{
    public class AppDbContext : DbContext
    {
        public DbSet<Clinica> Clinicas { get; set; }
        public DbSet<Agendamento> Agendamentos { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Consulta> Consultas { get; set; }

        public DbSet<Paciente> Pacientes { get; set; }
    }
}