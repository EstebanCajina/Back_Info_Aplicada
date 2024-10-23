using Microsoft.AspNetCore.Mvc;
using microserviceAuth.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using microserviceAuth.Models.microserviceAuth.Models;

[Route("api/[controller]")]
[ApiController]
public class BlocksController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BlocksController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatestBlock()
    {
        var block = await _context.Blocks
            .Include(b => b.Documents)
            .OrderByDescending(b => b.Id)
            .FirstOrDefaultAsync();

        if (block == null)
        {
            return Ok(new { id = 1, documents = new List<Document>() });
        }

        return Ok(block);
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateNewBlock()
    {
        var newBlock = new Block
        {
            MinedAt = DateTime.Now,
            Proof = 0,
            Documents = new List<Document>(),
            PreviousHash = "0",
            Hash = "0"
        };

        _context.Blocks.Add(newBlock);
        await _context.SaveChangesAsync();

        return Ok(newBlock);
    }
}
