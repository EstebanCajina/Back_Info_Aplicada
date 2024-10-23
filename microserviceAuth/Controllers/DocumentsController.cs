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

        // Guardar en la base de datos
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        return Ok("Document uploaded and saved to the database.");
    }

    [HttpGet("documents")]
    public async Task<IActionResult> GetDocuments()
    {
        // Proyección directa solo para los campos necesarios
        var documents = await _context.Documents
            .Select(d => new
            {
                d.Id,
                d.Owner,
                d.FileType,
                d.CreatedAt,
                d.Size
            })
            .ToListAsync();

        return Ok(documents); // Retornar los documentos sin Base64Content
    }

    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        var document = await _context.Documents.FindAsync(id);
        if (document == null)
        {
            return NotFound("Document not found.");
        }

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();

        return Ok("Document deleted successfully.");
    }
 


}
