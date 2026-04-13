using CIVS_certi.Data;
using CIVS_certi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CIVS_certi.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var hoy = DateTime.Today;

            // Estadísticas para el dashboard
            ViewBag.TotalPacientes = await _context.Pacientes
                .CountAsync(p => p.PacienteEstado);

            ViewBag.TotalMedicos = await _context.Medicos
                .CountAsync(m => m.MedicoEstado);

            ViewBag.CitasHoy = await _context.Citas
                .CountAsync(c => c.CitaFechaInicio.Date == hoy);

            ViewBag.CitasProgramadas = await _context.Citas
                .CountAsync(c => c.EstadoCita == EstadoCita.programada
                              && c.CitaFechaInicio.Date >= hoy);

            // Próximas 5 citas del día
            var proximasCitas = await _context.Citas
                .Include(c => c.Paciente)
                .Include(c => c.Medico)
                    .ThenInclude(m => m.Especialidad)
                .Where(c => c.CitaFechaInicio.Date == hoy
                         && c.EstadoCita != EstadoCita.cancelada)
                .OrderBy(c => c.CitaFechaInicio)
                .Take(5)
                .ToListAsync();

            return View(proximasCitas);
        }
    }
}