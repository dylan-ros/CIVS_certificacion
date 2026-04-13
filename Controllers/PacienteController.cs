using CIVS_certi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CIVS_certi.Models;

namespace CIVS_certi.Controllers
{
    public class PacienteController : Controller
    {
        private readonly AppDbContext _context;

        public PacienteController(AppDbContext context)
        {
            _context = context;
        }

        // GET --> Búsqueda de pacientes
        public async Task<IActionResult> Index(string? q)
        {
            var query = _context.Pacientes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim().ToLower();

                var palabras = q
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                query = query.Where(p =>
                    p.PacienteEstado &&
                    (
                        p.PacienteDPI.Contains(q) ||
                        p.PacienteTelefono.Contains(q) ||
                        (p.PacienteCorreo != null && p.PacienteCorreo.ToLower().Contains(q)) ||
                        (p.PacienteDireccion != null && p.PacienteDireccion.ToLower().Contains(q)) ||
                        palabras.All(x =>
                            (p.PacienteNombres + " " + p.PacienteApellido).ToLower().Contains(x)
                        )
                    )
                );
            }
            else
            {
                query = query.Where(p => p.PacienteEstado);
            }

            var pacientes = await query
                .OrderByDescending(p => p.PacienteFechaRegistro)
                .ToListAsync();

            ViewBag.Q = q;
            return View(pacientes);
        }

        // GET -> Mostrar formulario de creación
        [HttpGet]
        public IActionResult Crear()
        {
            return View("CrearPaciente");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Paciente paciente)
        {
            if (await _context.Pacientes.AnyAsync(p => p.PacienteDPI == paciente.PacienteDPI))
            {
                ModelState.AddModelError(nameof(paciente.PacienteDPI),
                    "Ya existe un paciente con este DPI");
            }

            if (!ModelState.IsValid)
            {
                return View("CrearPaciente", paciente);
            }

            paciente.PacienteEstado = true;
            paciente.PacienteFechaRegistro = DateTime.UtcNow;

            _context.Pacientes.Add(paciente);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Paciente {paciente.PacienteNombres} {paciente.PacienteApellido} creado con éxito.";

            return RedirectToAction(nameof(Crear));
        }

        // GET -> Detalle de paciente con citas
        public async Task<IActionResult> Detalle(int id)
        {
            var paciente = await _context.Pacientes
                .FirstOrDefaultAsync(p => p.PacienteId == id);

            if (paciente == null)
                return NotFound();

            var citas = await _context.Citas
                .Include(c => c.Medico)
                .ThenInclude(m => m.Especialidad)
                .Where(c => c.PacienteId == id)
                .OrderByDescending(c => c.CitaFechaInicio)
                .ToListAsync();

            ViewBag.Citas = citas;
            return View(paciente);
        }
    }
}