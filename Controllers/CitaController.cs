using CIVS_certi.Data;
using CIVS_certi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CIVS_certi.Controllers
{
    public class CitaController : Controller
    {
        private readonly AppDbContext _context;

        public CitaController(AppDbContext context)
        {
            _context = context;
        }

        // GET -> Consulta de citas
        public async Task<IActionResult> Index(string? q, string? estado)
        {
            var query = _context.Citas
                .Include(c => c.Paciente)
                .Include(c => c.Medico)
                    .ThenInclude(m => m.Especialidad)
                .AsQueryable();

            var (textoBusqueda, estadoDetectadoDesdeTexto) = ExtraerTextoYEstado(q);

            EstadoCita? estadoDropdown = null;
            if (!string.IsNullOrWhiteSpace(estado) &&
                Enum.TryParse<EstadoCita>(estado, true, out var estadoEnum))
            {
                estadoDropdown = estadoEnum;
            }

            if (!string.IsNullOrWhiteSpace(textoBusqueda))
            {
                var terminos = textoBusqueda
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim().ToLower())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();

                foreach (var termino in terminos)
                {
                    var t = termino;

                    query = query.Where(c =>
                        (
                            (
                                ((c.Paciente.PacienteNombres ?? "").Trim() + " " + (c.Paciente.PacienteApellido ?? "").Trim())
                                    .ToLower()
                                    .Contains(t)
                            )
                            ||
                            (
                                ((c.Paciente.PacienteApellido ?? "").Trim() + " " + (c.Paciente.PacienteNombres ?? "").Trim())
                                    .ToLower()
                                    .Contains(t)
                            )
                            ||
                            (
                                ((c.Medico.MedicoNombres ?? "").Trim() + " " + (c.Medico.MedicoApellidos ?? "").Trim())
                                    .ToLower()
                                    .Contains(t)
                            )
                            ||
                            (
                                ((c.Medico.MedicoApellidos ?? "").Trim() + " " + (c.Medico.MedicoNombres ?? "").Trim())
                                    .ToLower()
                                    .Contains(t)
                            )
                            ||
                            ((c.Paciente.PacienteDPI ?? "").ToLower().Contains(t))
                        )
                    );
                }
            }

            var estadoFinal = estadoDropdown ?? estadoDetectadoDesdeTexto;

            if (estadoFinal.HasValue)
            {
                query = query.Where(c => c.EstadoCita == estadoFinal.Value);
            }

            var citas = await query
                .OrderByDescending(c => c.CitaFechaInicio)
                .ToListAsync();

            ViewBag.Q = q;
            ViewBag.Estado = estado;
            ViewBag.EsMedico = false;

            return View(citas);
        }

        // GET -> Mostrar formulario/calendario para crear cita
        [HttpGet]
        public async Task<IActionResult> CrearCitas()
        {
            await CargarDatosParaCalendario();
            return View();
        }

        // GET -> JSON del calendario
        public async Task<IActionResult> EventosCalendario(int? medicoId, int? especialidadId)
        {
            var citasQuery = _context.Citas
                .Include(c => c.Paciente)
                .Include(c => c.Medico)
                    .ThenInclude(m => m.Especialidad)
                .Where(c => c.EstadoCita != EstadoCita.cancelada);

            if (medicoId.HasValue && medicoId > 0)
            {
                citasQuery = citasQuery.Where(c => c.MedicoId == medicoId.Value);
            }

            if (especialidadId.HasValue && especialidadId > 0)
            {
                citasQuery = citasQuery.Where(c => c.Medico.EspecialidadId == especialidadId.Value);
            }

            var citas = await citasQuery.ToListAsync();

            var eventos = citas.Select(c => new
            {
                id = c.CitaId,
                title = $"{c.Paciente.PacienteNombres} {c.Paciente.PacienteApellido}",
                start = c.CitaFechaInicio.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = c.CitaFechafin.ToString("yyyy-MM-ddTHH:mm:ss"),

                backgroundColor = c.EstadoCita switch
                {
                    EstadoCita.programada => "#f3df9b",
                    EstadoCita.confirmada => "#3b82f6",
                    EstadoCita.atendida => "#10b981",
                    EstadoCita.noAsistio => "#6b7280",
                    _ => "#6b7280"
                },

                borderColor = c.EstadoCita switch
                {
                    EstadoCita.programada => "#e0c35a",
                    EstadoCita.confirmada => "#2563eb",
                    EstadoCita.atendida => "#059669",
                    EstadoCita.noAsistio => "#4b5563",
                    _ => "#4b5563"
                },

                textColor = c.EstadoCita switch
                {
                    EstadoCita.programada => "#7a5a00",
                    _ => "#ffffff"
                },

                extendedProps = new
                {
                    tipo = "cita",
                    pacienteId = c.PacienteId,
                    pacienteNombre = $"{c.Paciente.PacienteNombres} {c.Paciente.PacienteApellido}",
                    pacienteDpi = c.Paciente.PacienteDPI,
                    medicoId = c.MedicoId,
                    medicoNombre = $"Dr. {c.Medico.MedicoNombres} {c.Medico.MedicoApellidos}",
                    especialidad = c.Medico.Especialidad?.EspecialidadNombre ?? "",
                    motivo = c.CitaMotivo,
                    estado = c.EstadoCita.ToString()
                }
            });

            return Json(eventos);
        }

        // POST -> Crear cita
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearCitas(Cita cita)
        {
            if (cita.PacienteId == 0)
                ModelState.AddModelError(nameof(cita.PacienteId), "Debe seleccionar un paciente.");

            if (cita.MedicoId == 0)
                ModelState.AddModelError(nameof(cita.MedicoId), "Debe seleccionar un médico.");

            if (string.IsNullOrWhiteSpace(cita.CitaMotivo))
                ModelState.AddModelError(nameof(cita.CitaMotivo), "Debe ingresar el motivo de la consulta.");

            var zonaGuatemala = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
            var ahoraGuatemala = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaGuatemala);

            if (cita.CitaFechaInicio <= ahoraGuatemala)
            {
                ModelState.AddModelError(nameof(cita.CitaFechaInicio),
                    "No se puede agendar una cita en el pasado.");
            }

            if (cita.CitaFechafin <= cita.CitaFechaInicio)
            {
                ModelState.AddModelError(nameof(cita.CitaFechafin),
                    "La hora de fin debe ser posterior a la hora de inicio.");
            }

            if (cita.CitaFechafin > cita.CitaFechaInicio)
            {
                var duracion = (cita.CitaFechafin - cita.CitaFechaInicio).TotalMinutes;
                if (duracion < 30)
                {
                    ModelState.AddModelError(nameof(cita.CitaFechafin),
                        "La cita debe durar al menos 30 minutos.");
                }
            }

            if (cita.CitaFechaInicio.DayOfWeek == DayOfWeek.Sunday)
            {
                ModelState.AddModelError(nameof(cita.CitaFechaInicio),
                    "No se pueden agendar citas en domingo.");
            }

            if (cita.MedicoId > 0)
            {
                bool tieneChoque = await _context.Citas.AnyAsync(c =>
                    c.MedicoId == cita.MedicoId &&
                    c.EstadoCita != EstadoCita.cancelada &&
                    c.CitaFechaInicio < cita.CitaFechafin &&
                    c.CitaFechafin > cita.CitaFechaInicio
                );

                if (tieneChoque)
                {
                    ModelState.AddModelError(string.Empty,
                        "El médico ya tiene una cita agendada en ese horario.");
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarDatosParaCalendario();
                TempData["Error"] = "Por favor corrija los errores del formulario.";
                return View(cita);
            }

            cita.EstadoCita = EstadoCita.programada;
            cita.CitaFechaCreada = DateTime.UtcNow;

            _context.Citas.Add(cita);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cita agendada exitosamente.";
            return RedirectToAction(nameof(CrearCitas));
        }

        // POST -> Cambiar estado
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id, EstadoCita nuevoEstado)
        {
            var cita = await _context.Citas.FindAsync(id);

            if (cita == null)
                return NotFound();

            cita.EstadoCita = nuevoEstado;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Estado de la cita actualizado a: {nuevoEstado}";
            return RedirectToAction(nameof(Index));
        }

        // POST -> Cancelar cita
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var cita = await _context.Citas.FindAsync(id);

            if (cita == null)
                return NotFound();

            cita.EstadoCita = EstadoCita.cancelada;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cita cancelada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task CargarDatosParaCalendario()
        {
            var pacientes = await _context.Pacientes
                .Where(p => p.PacienteEstado)
                .OrderBy(p => p.PacienteNombres)
                .Select(p => new
                {
                    id = p.PacienteId,
                    nombre = $"{p.PacienteNombres} {p.PacienteApellido}",
                    dpi = p.PacienteDPI,
                    telefono = p.PacienteTelefono
                })
                .ToListAsync();

            ViewBag.PacientesJson = JsonSerializer.Serialize(pacientes, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var medicos = await _context.Medicos
                .Include(m => m.Especialidad)
                .Where(m => m.MedicoEstado)
                .OrderBy(m => m.MedicoNombres)
                .Select(m => new
                {
                    id = m.MedicoId,
                    nombre = $"Dr. {m.MedicoNombres} {m.MedicoApellidos}",
                    colegiado = m.MedicoColegiado,
                    especialidad = m.Especialidad!.EspecialidadNombre,
                    especialidadId = m.EspecialidadId
                })
                .ToListAsync();

            ViewBag.MedicosJson = JsonSerializer.Serialize(medicos, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            ViewBag.Especialidades = await _context.Especialidades
                .Where(e => e.EspecialidadEstado)
                .OrderBy(e => e.EspecialidadNombre)
                .ToListAsync();
        }





        private (string textoLimpio, EstadoCita? estadoDetectado) ExtraerTextoYEstado(string? q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return (string.Empty, null);

            var texto = q.Trim().ToLower();
            texto = string.Join(" ", texto.Split(' ', StringSplitOptions.RemoveEmptyEntries));

            EstadoCita? estadoDetectado = null;

            if (texto.Contains("no asistio"))
            {
                estadoDetectado = EstadoCita.noAsistio;
                texto = texto.Replace("no asistio", " ");
            }
            else if (texto.Contains("noasistio"))
            {
                estadoDetectado = EstadoCita.noAsistio;
                texto = texto.Replace("noasistio", " ");
            }
            else if (texto.Contains("programada"))
            {
                estadoDetectado = EstadoCita.programada;
                texto = texto.Replace("programada", " ");
            }
            else if (texto.Contains("confirmada"))
            {
                estadoDetectado = EstadoCita.confirmada;
                texto = texto.Replace("confirmada", " ");
            }
            else if (texto.Contains("atendida"))
            {
                estadoDetectado = EstadoCita.atendida;
                texto = texto.Replace("atendida", " ");
            }
            else if (texto.Contains("cancelada"))
            {
                estadoDetectado = EstadoCita.cancelada;
                texto = texto.Replace("cancelada", " ");
            }

            texto = texto.Replace("estado", " ");
            texto = string.Join(" ", texto.Split(' ', StringSplitOptions.RemoveEmptyEntries));

            return (texto, estadoDetectado);
        }


    }
}