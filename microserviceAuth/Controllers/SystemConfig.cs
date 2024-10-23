using Microsoft.AspNetCore.Mvc;
using microserviceAuth.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using microserviceAuth.Models.microserviceAuth.Models;

[Route("api/[controller]")]
[ApiController]
public class SystemConfigController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SystemConfigController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("get")]
    public async Task<IActionResult> GetSystemConfig()
    {
        var config = await _context.SystemConfigs.FirstOrDefaultAsync();
        if (config == null)
        {
            return NotFound("No system configuration found.");
        }
        return Ok(config);
    }

    [HttpPost("add")]
    public async Task<IActionResult> AddSystemConfig([FromBody] SystemConfigDto configDto)
    {
        var config = await _context.SystemConfigs.FirstOrDefaultAsync();
        if (config != null)
        {
            return BadRequest("Configuration already exists. Use the edit method to update it.");
        }

        var newConfig = new SystemConfig
        {
            MaxDocs = configDto.MaxDocs,
            ProcessTime = configDto.ProcessTime,
            QuantityOfZeros = configDto.QuantityOfZeros
        };

        _context.SystemConfigs.Add(newConfig);
        await _context.SaveChangesAsync();
        return Ok("System configuration added successfully.");
    }

    [HttpPost("edit")]
    public async Task<IActionResult> EditSystemConfig([FromBody] SystemConfigDto configDto)
    {
        var config = await _context.SystemConfigs.FirstOrDefaultAsync();
        if (config == null)
        {
            return NotFound("No configuration found to update.");
        }

        config.MaxDocs = configDto.MaxDocs;
        config.ProcessTime = configDto.ProcessTime;
        config.QuantityOfZeros = configDto.QuantityOfZeros;

        _context.SystemConfigs.Update(config);
        await _context.SaveChangesAsync();
        return Ok("System configuration updated successfully.");
    }
}
