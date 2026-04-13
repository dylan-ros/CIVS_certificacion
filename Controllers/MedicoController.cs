using CIVS_certi.Data;
using CIVS_certi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CIVS_certi.Controllers
{
    public class MedicoController : Controller
    {
        private readonly AppDbContext _context;

        public MedicoController(AppDbContext context)
        {
            _context = context;
        }

        // GET -> Buscar médico
        public async Task<IActionResult> Index(string? q)
        {
            var query = _context.Medicos
                .Include(m => m.Especialidad)
                .Where(m => m.MedicoEstado)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim().ToLower();

                var palabras = q
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                query = query.Where(m =>
                    m.MedicoNombres.ToLower().Contains(q) ||
                    m.MedicoApellidos.ToLower().Contains(q) ||
                    m.MedicoColegiado.ToLower().Contains(q) ||
                    m.MedicoTelefono.ToLower().Contains(q) ||
                    (m.MedicoEmail != null && m.MedicoEmail.ToLower().Contains(q)) ||
                    (m.Especialidad != null && m.Especialidad.EspecialidadNombre.ToLower().Contains(q)) ||
                    palabras.All(x =>
                        (m.MedicoNombres + " " + m.MedicoApellidos).ToLower().Contains(x)
                    )
                );
            }

            var medicos = await query
                .OrderBy(m => m.MedicoNombres)
                .ToListAsync();

            ViewBag.Q = q;
            return View(medicos);
        }

        // GET -> Prepara la vista para crear un médico
        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            await CargarEspecialidades();
            return View();
        }

        // POST -> Crea al médico
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Medico medico)
        {
            if (await _context.Medicos.AnyAsync(m => m.MedicoColegiado == medico.MedicoColegiado))
            {
                ModelState.AddModelError(nameof(medico.MedicoColegiado),
                    "Ya existe un médico con ese colegiado");
            }

            if (!ModelState.IsValid)
            {
                await CargarEspecialidades(medico.EspecialidadId);
                return View(medico);
            }

            medico.MedicoEstado = true;
            medico.MedicoFechaRegistro = DateTime.UtcNow;

            _context.Medicos.Add(medico);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Dr. {medico.MedicoNombres} {medico.MedicoApellidos} se ha registrado correctamente.";
            return RedirectToAction(nameof(Crear));
        }

        // Helper para cargar especialidades
        private async Task CargarEspecialidades(int? seleccionado = null)
        {
            ViewBag.Especialidades = new SelectList(
                await _context.Especialidades
                    .Where(e => e.EspecialidadEstado)
                    .OrderBy(e => e.EspecialidadNombre)
                    .ToListAsync(),
                "EspecialidadId",
                "EspecialidadNombre",
                seleccionado
            );
        }
    }
}