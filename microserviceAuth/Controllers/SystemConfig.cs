namespace microserviceAuth.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using microserviceAuth.Models;
    using microserviceAuth.Services; // Importar AuditService
    using Microsoft.EntityFrameworkCore;
    using System.Threading.Tasks;
    using microserviceAuth.Models.microserviceAuth.Models;

    [Route("api/[controller]")]
    [ApiController]
    public class SystemConfigController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService; // Inyección de AuditService

        public SystemConfigController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService; // Inicialización de AuditService
        }

        [HttpGet("get")]
        public async Task<IActionResult> GetSystemConfig()
        {
            var config = await _context.SystemConfigs.FirstOrDefaultAsync();
            if (config == null)
            {
                await _auditService.LogActionAsync("Intento de consulta fallido: Configuración del sistema no encontrada.");
                return NotFound("No se encontró la configuración del sistema.");
            }

            return Ok(config);
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddSystemConfig([FromBody] SystemConfigDto configDto)
        {
            var config = await _context.SystemConfigs.FirstOrDefaultAsync();
            if (config != null)
            {
                await _auditService.LogActionAsync("Intento de adición fallido: La configuración ya existe.");
                return BadRequest("La configuración ya existe. Use el método de edición para actualizarla.");
            }

            var newConfig = new SystemConfig
            {
                MaxDocs = configDto.MaxDocs,
                ProcessTime = configDto.ProcessTime,
                QuantityOfZeros = configDto.QuantityOfZeros
            };

            _context.SystemConfigs.Add(newConfig);
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync("Configuración del sistema añadida exitosamente.");
            return Ok("Configuración del sistema añadida exitosamente.");
        }

        [HttpPost("edit")]
        public async Task<IActionResult> EditSystemConfig([FromBody] SystemConfigDto configDto)
        {
            var config = await _context.SystemConfigs.FirstOrDefaultAsync();
            if (config == null)
            {
                await _auditService.LogActionAsync("Intento de edición fallido: Configuración no encontrada.");
                return NotFound("No se encontró la configuración para actualizar.");
            }

            config.MaxDocs = configDto.MaxDocs;
            config.ProcessTime = configDto.ProcessTime;
            config.QuantityOfZeros = configDto.QuantityOfZeros;

            _context.SystemConfigs.Update(config);
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync("Configuración del sistema actualizada exitosamente.");
            return Ok("Configuración del sistema actualizada exitosamente.");
        }
    }
}
