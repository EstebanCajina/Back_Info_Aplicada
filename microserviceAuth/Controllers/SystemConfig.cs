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
        private readonly IAuditService _auditService; // Inyecci�n de AuditService

        public SystemConfigController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService; // Inicializaci�n de AuditService
        }

        [HttpGet("get")]
        public async Task<IActionResult> GetSystemConfig()
        {
            var config = await _context.SystemConfigs.FirstOrDefaultAsync();
            if (config == null)
            {
                await _auditService.LogActionAsync("Intento de consulta fallido: Configuraci�n del sistema no encontrada.");
                return NotFound("No se encontr� la configuraci�n del sistema.");
            }

            return Ok(config);
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddSystemConfig([FromBody] SystemConfigDto configDto)
        {
            var config = await _context.SystemConfigs.FirstOrDefaultAsync();
            if (config != null)
            {
                await _auditService.LogActionAsync("Intento de adici�n fallido: La configuraci�n ya existe.");
                return BadRequest("La configuraci�n ya existe. Use el m�todo de edici�n para actualizarla.");
            }

            var newConfig = new SystemConfig
            {
                MaxDocs = configDto.MaxDocs,
                ProcessTime = configDto.ProcessTime,
                QuantityOfZeros = configDto.QuantityOfZeros
            };

            _context.SystemConfigs.Add(newConfig);
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync("Configuraci�n del sistema a�adida exitosamente.");
            return Ok("Configuraci�n del sistema a�adida exitosamente.");
        }

        [HttpPost("edit")]
        public async Task<IActionResult> EditSystemConfig([FromBody] SystemConfigDto configDto)
        {
            var config = await _context.SystemConfigs.FirstOrDefaultAsync();
            if (config == null)
            {
                await _auditService.LogActionAsync("Intento de edici�n fallido: Configuraci�n no encontrada.");
                return NotFound("No se encontr� la configuraci�n para actualizar.");
            }

            config.MaxDocs = configDto.MaxDocs;
            config.ProcessTime = configDto.ProcessTime;
            config.QuantityOfZeros = configDto.QuantityOfZeros;

            _context.SystemConfigs.Update(config);
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync("Configuraci�n del sistema actualizada exitosamente.");
            return Ok("Configuraci�n del sistema actualizada exitosamente.");
        }
    }
}
