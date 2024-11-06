using Microsoft.AspNetCore.Mvc;
using microserviceAuth.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using microserviceAuth.Models.microserviceAuth.Models;
using System.Security.Claims;
using System.IO.Compression;

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
            OwnerId = documentDto.OwnerId,
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

    [HttpGet("documents/{userId}")]
    public async Task<IActionResult> GetDocuments(string userId)
    {
        // Proyección directa solo para los campos necesarios
        var documents = await _context.Documents
            .Where(d => d.OwnerId == userId) // Filtrar documentos por ID de propietario
            .Select(d => new
            {
                d.Id,
                Owner = User.Identity.Name, // Suponiendo que el nombre del usuario está disponible
                d.OwnerId,
                d.FileType,
                d.CreatedAt,
                d.Size,
                d.BlockId
            })
            .ToListAsync();

        return Ok(documents); // Retornar los documentos
    }

    [HttpGet("download/{id}")]
    public async Task<IActionResult> GetDocumentById(int id)
    {
        // Buscar el documento en la base de datos
        var document = await _context.Documents
            .Where(d => d.Id == id)
            .Select(d => new
            {
                d.Id,
                d.OwnerId,
                d.FileType,
                d.Base64Content // Incluir el contenido en Base64 para la descarga
            })
            .FirstOrDefaultAsync();

        if (document == null)
        {
            return NotFound("Document not found.");
        }

        // Retornar el contenido del documento
        return Ok(new
        {
            base64Content = document.Base64Content,
            fileType = document.FileType
        });
    }

    [HttpDelete("delete/multiple")]
    public async Task<IActionResult> DeleteMultipleDocuments([FromBody] List<int> ids)
    {
        var documents = await _context.Documents.Where(d => ids.Contains(d.Id)).ToListAsync();
        if (documents.Count == 0)
        {
            return NotFound("No se encontraron documentos para eliminar.");
        }

        _context.Documents.RemoveRange(documents);
        await _context.SaveChangesAsync();

        return Ok("Documentos eliminados exitosamente.");
    }

    [HttpPost("download/zip")]
    public async Task<IActionResult> DownloadMultipleDocuments([FromBody] List<int> ids)
    {
        var documents = await _context.Documents.Where(d => ids.Contains(d.Id)).ToListAsync();
        if (!documents.Any())
        {
            return NotFound("No se encontraron documentos para descargar.");
        }

        // Crear un archivo ZIP
        using (var memoryStream = new MemoryStream())
        {
            using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var document in documents)
                {
                    var zipEntry = zipArchive.CreateEntry($"document_{document.Id}.{document.FileType.Split('/')[1]}", CompressionLevel.Optimal);
                    using (var entryStream = zipEntry.Open())
                    {
                        var fileBytes = Convert.FromBase64String(document.Base64Content);
                        await entryStream.WriteAsync(fileBytes, 0, fileBytes.Length);
                    }
                }
            }

            return File(memoryStream.ToArray(), "application/zip", "documents.zip");
        }
    }


    [HttpGet("block/{blockId}")]
    public async Task<IActionResult> GetDocumentsByBlockId(int blockId)
    {
        var documents = await _context.Documents
            .Where(d => d.BlockId == blockId)
            .Select(d => new
            {
                d.Id,
                d.OwnerId,
                d.FileType,
                d.CreatedAt,
                d.Size,
                d.BlockId
            })
            .ToListAsync();

        if (!documents.Any())
        {
            return NotFound("No se encontraron documentos para el bloque especificado.");
        }

        return Ok(documents);
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
