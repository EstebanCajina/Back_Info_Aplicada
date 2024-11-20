namespace microserviceAuth.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using microserviceAuth.Models;
    using microserviceAuth.Services; // Importar AuditService
    using Microsoft.EntityFrameworkCore;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using microserviceAuth.Models.microserviceAuth.Models;
    using System.Security.Claims;
    using System.IO.Compression;
    using microserviceAuth.Encrypted;
    using System;

    [Route("api/[controller]")]
    [ApiController]
    public class DocumentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly AesEncryption _aesEncryption;
        private readonly IAuditService _auditService; // Inyección de AuditService

        public DocumentsController(ApplicationDbContext context, AesEncryption aesEncryption, IAuditService auditService)
        {
            _context = context;
            _aesEncryption = aesEncryption;
            _auditService = auditService; // Inicialización de AuditService
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument([FromBody] DocumentDto documentDto)
        {
            string encryptedContent = _aesEncryption.Encrypt(documentDto.Base64Content);

            var document = new Document
            {
                OwnerId = documentDto.OwnerId,
                FileType = documentDto.FileType,
                CreatedAt = DateTime.Now,
                Size = documentDto.Size,
                Base64Content = encryptedContent
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync($"Documento subido y guardado. ID del documento: {document.Id}");
            return Ok("Documento subido y guardado en la base de datos.");
        }

        [HttpGet("documents/{userId}")]
        public async Task<IActionResult> GetDocuments(string userId)
        {
            var documents = await _context.Documents
                .Where(d => d.OwnerId == userId)
                .Select(d => new
                {
                    d.Id,
                    Owner = User.Identity!.Name,
                    d.OwnerId,
                    d.FileType,
                    d.CreatedAt,
                    d.Size,
                    d.BlockId
                })
                .ToListAsync();

            return Ok(documents);
        }

        [HttpGet("download/{id}")]
        public async Task<IActionResult> GetDocumentById(int id)
        {
            var document = await _context.Documents
                .Where(d => d.Id == id)
                .Select(d => new
                {
                    d.Id,
                    d.OwnerId,
                    d.FileType,
                    d.Base64Content
                })
                .FirstOrDefaultAsync();

            if (document == null)
            {
                await _auditService.LogActionAsync($"Intento de descarga fallido: Documento no encontrado. ID del documento: {id}");
                return NotFound("Documento no encontrado.");
            }

            string decryptedContent = _aesEncryption.Decrypt(document.Base64Content);

            await _auditService.LogActionAsync($"Documento descargado. ID del documento: {id}, Usuario: {document.OwnerId}");
            return Ok(new
            {
                base64Content = decryptedContent,
                fileType = document.FileType
            });
        }

        [HttpDelete("delete/multiple")]
        public async Task<IActionResult> DeleteMultipleDocuments([FromBody] List<int> ids)
        {
            var documents = await _context.Documents.Where(d => ids.Contains(d.Id)).ToListAsync();
            if (documents.Count == 0)
            {
                await _auditService.LogActionAsync($"Intento de eliminación fallido: No se encontraron documentos para eliminar. IDs: {string.Join(", ", ids)}");
                return NotFound("No se encontraron documentos para eliminar.");
            }

            _context.Documents.RemoveRange(documents);
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync($"Documentos eliminados exitosamente. IDs: {string.Join(", ", ids)}");
            return Ok("Documentos eliminados exitosamente.");
        }

        [HttpPost("download/zip")]
        public async Task<IActionResult> DownloadMultipleDocuments([FromBody] List<int> ids)
        {
            var documents = await _context.Documents.Where(d => ids.Contains(d.Id)).ToListAsync();
            if (documents.Count == 0)
            {
                await _auditService.LogActionAsync($"Intento de descarga en ZIP fallido: No se encontraron documentos. IDs: {string.Join(", ", ids)}");
                return NotFound("No se encontraron documentos para descargar.");
            }

            using (var memoryStream = new MemoryStream())
            {
                using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var document in documents)
                    {
                        var fileTypeExtension = document.FileType.Split('/')[1];
                        var zipEntry = zipArchive.CreateEntry($"document_{document.Id}.{fileTypeExtension}", CompressionLevel.Optimal);

                        using (var entryStream = zipEntry.Open())
                        {
                            var decryptedContent = _aesEncryption.Decrypt(document.Base64Content);
                            var fileBytes = Convert.FromBase64String(decryptedContent);

                            await entryStream.WriteAsync(fileBytes.AsMemory(), CancellationToken.None);
                        }
                    }
                }

                await _auditService.LogActionAsync($"Documentos descargados en ZIP. IDs: {string.Join(", ", ids)}");
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

            if (documents.Count == 0)
            {
                await _auditService.LogActionAsync($"Intento de consulta fallido: No se encontraron documentos para el bloque especificado. ID del bloque: {blockId}");
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
                await _auditService.LogActionAsync($"Intento de eliminación fallido: Documento no encontrado. ID del documento: {id}");
                return NotFound("Documento no encontrado.");
            }

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync($"Documento eliminado exitosamente. ID del documento: {id}");
            return Ok("Documento eliminado exitosamente.");
        }
    }
}
