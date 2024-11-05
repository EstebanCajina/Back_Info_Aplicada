using Microsoft.AspNetCore.Mvc;
using microserviceAuth.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using microserviceAuth.Models.microserviceAuth.Models;
using System.Text;
using System.Security.Cryptography;


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

    [HttpPost("create/{maxDocs:int}")]
    public async Task<IActionResult> CreateNewBlock(int maxDocs)
    {
        // Verificar el último bloque creado para asignar el hash previo
        var lastBlock = await _context.Blocks.OrderByDescending(b => b.Id).FirstOrDefaultAsync();
        string previousHash = lastBlock != null ? lastBlock.Hash : new string('0', 64);

        var newBlock = new Block
        {
            Proof = 0,
            PreviousHash = previousHash,
            Hash = "0", // Placeholder que será actualizado luego
            Documents = new List<Document>()
        };

        _context.Blocks.Add(newBlock);
        await _context.SaveChangesAsync();

        // Obtener documentos según maxDocs y asignarlos al nuevo bloque
        var documents = await _context.Documents
            .Where(d => d.BlockId == null)
            .Take(maxDocs)
            .ToListAsync();

        foreach (var document in documents)
        {
            document.BlockId = newBlock.Id; // Asigna el ID del nuevo bloque a los documentos
        }
        await _context.SaveChangesAsync();

        // Calcular el hash del bloque con SHA-256
        string concatenatedData = $"{newBlock.MinedAt}-{newBlock.Proof}-{newBlock.Milliseconds}-{newBlock.PreviousHash}";
        foreach (var doc in documents)
        {
            concatenatedData += $"-{doc.FileType}-{doc.CreatedAt}-{doc.Size}-{doc.Base64Content}";
        }

        newBlock.Hash = ComputeSha256Hash(concatenatedData); // Genera el hash y asigna al bloque
        await _context.SaveChangesAsync();

        return Ok("newBlock");
    }

    // Método para calcular SHA-256
    private static string ComputeSha256Hash(string rawData)
    {
        using (var sha256Hash = SHA256.Create())
        {
            var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }

}
