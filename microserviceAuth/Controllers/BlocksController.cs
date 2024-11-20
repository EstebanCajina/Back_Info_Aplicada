namespace microserviceAuth.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using microserviceAuth.Models;
    using microserviceAuth.Services; // Importar AuditService
    using Microsoft.EntityFrameworkCore;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using microserviceAuth.Models.microserviceAuth.Models;
    using System.Security.Cryptography;

    [Route("api/[controller]")]
    [ApiController]
    public class BlocksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService; // Inyección de AuditService

        public BlocksController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService; // Inicialización de AuditService
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllBlocks()
        {
            var blocks = await _context.Blocks
                .Include(b => b.Documents)
                .OrderBy(b => b.Id)
                .Select(b => new
                {
                    b.Id,
                    b.MinedAt,
                    b.Proof,
                    b.Milliseconds,
                    b.PreviousHash,
                    b.Hash,
                    b.IsMined,
                    Documents = b.Documents.Select(d => new
                    {
                        d.Id,
                        d.OwnerId,
                        d.FileType,
                        d.CreatedAt,
                        d.Size,
                        d.BlockId
                    }).ToList()
                })
                .ToListAsync();

            if (blocks.Count == 0)
            {
                await _auditService.LogActionAsync("Consulta de todos los bloques: no se encontraron bloques.");
                return NotFound("No se encontraron bloques.");
            }

            return Ok(blocks);
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
                await _auditService.LogActionAsync("Consulta del último bloque: no se encontró.");
                return Ok(new { id = 1, documents = new List<Document>() });
            }

            return Ok(block);
        }

        [HttpPost("create-and-mine/{maxDocs:int}/{numZeros:int}")]
        public async Task<IActionResult> CreateAndMineBlock(int maxDocs, int numZeros)
        {
            // Crear un nuevo bloque
            var lastBlock = await _context.Blocks.OrderByDescending(b => b.Id).FirstOrDefaultAsync();
            string previousHash = lastBlock != null ? lastBlock.Hash : new string('0', 64);

            var newBlock = new Block
            {
                Proof = 0,
                PreviousHash = previousHash,
                Hash = "0",
                Documents = new List<Document>(),
                IsMined = false,
                Milliseconds = 0
            };

            _context.Blocks.Add(newBlock);
            await _context.SaveChangesAsync();

            var documents = await _context.Documents
                .Where(d => d.BlockId == null)
                .Take(maxDocs)
                .ToListAsync();

            foreach (var document in documents)
            {
                document.BlockId = newBlock.Id;
            }
            await _context.SaveChangesAsync();

            string concatenatedData = $"{newBlock.MinedAt}-{newBlock.Proof}-{newBlock.Milliseconds}-{newBlock.PreviousHash}";
            var stringBuilder = new StringBuilder(concatenatedData);

            foreach (var doc in documents)
            {
                stringBuilder.AppendFormat("-{0}-{1}-{2}-{3}", doc.FileType, doc.CreatedAt, doc.Size, doc.Base64Content);
            }
            concatenatedData = stringBuilder.ToString();
            newBlock.Hash = ComputeSha256Hash(concatenatedData);
            await _context.SaveChangesAsync();

            var mineResult = await MineBlock(newBlock.Id, numZeros);
            if (mineResult is OkObjectResult okResult)
            {
                await _auditService.LogActionAsync($"Bloque creado y minado exitosamente. ID del bloque: {newBlock.Id}");
                return Ok(new
                {
                    Block = newBlock,
                    MiningResult = okResult.Value
                });
            }

            await _auditService.LogActionAsync($"Error al crear y minar el bloque. ID del bloque: {newBlock.Id}");
            return StatusCode(500, "Error durante el proceso de minería.");
        }

        private async Task<IActionResult> MineBlock(int blockId, int numZeros)
        {
            var block = await _context.Blocks
                .Include(b => b.Documents)
                .FirstOrDefaultAsync(b => b.Id == blockId);

            if (block == null)
            {
                await _auditService.LogActionAsync($"Intento de minería fallido: bloque no encontrado. ID del bloque: {blockId}");
                return NotFound("El bloque no existe.");
            }

            string previousHash = block.Hash;
            int proof = 0;
            int milliseconds = 0;
            string requiredPrefix = new string('0', numZeros);

            using (var cancellationTokenSource = new System.Threading.CancellationTokenSource())
            {
                var token = cancellationTokenSource.Token;
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    var millisecondsTask = Task.Run(() =>
                    {
                        while (!token.IsCancellationRequested)
                        {
                            milliseconds = (int)stopwatch.ElapsedMilliseconds;
                            System.Threading.Thread.Sleep(1);
                        }
                    }, token);

                    while (true)
                    {
                        DateTime minedAt = DateTime.UtcNow;
                        proof++;

                        var stringBuilder = new StringBuilder();
                        stringBuilder.AppendFormat("{0}-{1}-{2}-{3}", minedAt, proof, milliseconds, block.PreviousHash);

                        foreach (var doc in block.Documents)
                        {
                            stringBuilder.AppendFormat("-{0}-{1}-{2}-{3}", doc.FileType, doc.CreatedAt, doc.Size, doc.Base64Content);
                        }

                        string concatenatedData = stringBuilder.ToString();
                        string newHash = ComputeSha256Hash(concatenatedData);

                        if (newHash.StartsWith(requiredPrefix))
                        {
                            block.Hash = newHash;
                            block.MinedAt = minedAt;
                            block.Proof = proof;
                            block.Milliseconds = milliseconds;
                            block.IsMined = true;
                            block.LeadingZeros = numZeros;

                            await _context.SaveChangesAsync();

                            var blockToUpdate = await _context.Blocks
                                .FirstOrDefaultAsync(b => b.PreviousHash == previousHash);

                            if (blockToUpdate != null)
                            {
                                blockToUpdate.PreviousHash = newHash;
                                await _context.SaveChangesAsync();
                            }

                            cancellationTokenSource.Cancel();
                            await _auditService.LogActionAsync($"Bloque minado exitosamente. ID del bloque: {blockId}");
                            return Ok(new
                            {
                                BlockId = blockId,
                                NewHash = newHash,
                                MinedAt = minedAt,
                                Proof = proof,
                                Milliseconds = milliseconds,
                                LeadingZeros = block.LeadingZeros
                            });
                        }
                    }
                }
                finally
                {
                    stopwatch.Stop();
                }
            }
        }

        private static string ComputeSha256Hash(string rawData)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        [HttpGet("validate-chain")]
        public async Task<IActionResult> ValidateChain()
        {
            var blocks = await _context.Blocks
                .Include(b => b.Documents)
                .OrderBy(b => b.Id)
                .ToListAsync();

            var validationErrors = new List<object>();

            for (int i = 1; i < blocks.Count; i++)
            {
                var previousBlock = blocks[i - 1];
                var currentBlock = blocks[i];

                if (currentBlock.PreviousHash != previousBlock.Hash)
                {
                    validationErrors.Add(new { Id = currentBlock.Id, Error = "Hash previo inconsistente" });
                }

                string concatenatedData = $"{currentBlock.MinedAt}-{currentBlock.Proof}-{currentBlock.Milliseconds}-{currentBlock.PreviousHash}";
                var stringBuilder = new StringBuilder(concatenatedData);

                foreach (var doc in currentBlock.Documents)
                {
                    stringBuilder.AppendFormat("-{0}-{1}-{2}-{3}", doc.FileType, doc.CreatedAt, doc.Size, doc.Base64Content);
                }

                string recalculatedHash = ComputeSha256Hash(stringBuilder.ToString());

                if (!currentBlock.IsMined)
                {
                    if (currentBlock.Hash != recalculatedHash)
                    {
                        validationErrors.Add(new { Id = currentBlock.Id, Error = "El hash del bloque es inválido" });
                    }
                }
                else
                {
                    if (!currentBlock.Hash.StartsWith(new string('0', currentBlock.LeadingZeros)) || currentBlock.Hash != recalculatedHash)
                    {
                        validationErrors.Add(new { Id = currentBlock.Id, Error = $"El hash del bloque minado no cumple con los {currentBlock.LeadingZeros} ceros requeridos o es inválido" });
                    }
                }
            }

            if (validationErrors.Count == 0)
            {
                await _auditService.LogActionAsync("Cadena validada exitosamente: sin errores de validación.");
                return Ok(new { Message = "Cadena válida", IsValid = true });
            }

            await _auditService.LogActionAsync("Cadena de bloques con errores de validación encontrados.");
            return Ok(new { IsValid = false, Errors = validationErrors });
        }
    }
}
