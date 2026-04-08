using Microsoft.EntityFrameworkCore;

namespace CIVS_certi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Aquí irán tus DbSet
        // public DbSet<Paciente> Pacientes { get; set; }
    }
}