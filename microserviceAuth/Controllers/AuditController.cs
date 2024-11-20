namespace microserviceAuth.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using microserviceAuth.Models;
    using Microsoft.EntityFrameworkCore;
    using System.Threading.Tasks;

    [Route("api/[controller]")]
    [ApiController]
    public class AuditController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuditController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Endpoint para obtener todos los registros de auditoría
        [HttpGet("logs")]
        public async Task<IActionResult> GetAllLogs()
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(log => log.CreatedAt)
                .ToListAsync();

            if (logs.Count == 0)
            {
                return NotFound("No se encontraron registros de auditoría.");
            }

            return Ok(logs);
        }

        // Endpoint para eliminar todos los registros de auditoría
        [HttpDelete("logs")]
        public async Task<IActionResult> DeleteAllLogs()
        {
            var logs = await _context.AuditLogs.ToListAsync();

            if (logs.Count == 0)
            {
                return NotFound("No hay registros de auditoría para eliminar.");
            }

            _context.AuditLogs.RemoveRange(logs);
            await _context.SaveChangesAsync();

            return Ok("Todos los registros de auditoría han sido eliminados.");
        }
    }
}
