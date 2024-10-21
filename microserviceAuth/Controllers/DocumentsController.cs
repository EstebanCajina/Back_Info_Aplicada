using Microsoft.AspNetCore.Mvc;
using microserviceAuth.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using microserviceAuth.Models.microserviceAuth.Models;

[Route("api/[controller]")]
[ApiController]
public class DocumentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private static MemPool _memPool = new MemPool();

    public DocumentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadDocument([FromBody] DocumentDto documentDto)
    {
        var document = new Document
        {
            Owner = documentDto.Owner,
            FileType = documentDto.FileType,
            CreatedAt = DateTime.Now,
            Size = documentDto.Size,
            Base64Content = documentDto.Base64Content
        };

        // Agregar el documento a la MemPool
        _memPool.AddDocument(document);

        // Guardar en la base de datos
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        return Ok("Document uploaded and saved to MemPool and database.");
    }

    [HttpGet("mempool")]
    public IActionResult GetMemPool()
    {
        return Ok(_memPool.Documents); // Retornar los documentos en la MemPool
    }
}
