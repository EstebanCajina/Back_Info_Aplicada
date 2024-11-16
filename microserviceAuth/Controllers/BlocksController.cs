namespace microserviceAuth.Controllers
{

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


        [HttpGet("all")]
        public async Task<IActionResult> GetAllBlocks()
        {
            var blocks = await _context.Blocks
                .Include(b => b.Documents) // Incluye los documentos de cada bloque
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

            if (blocks.Count() == 0)
            {
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
                Documents = new List<Document>(),
                IsMined = false,
                Milliseconds = 0
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
            var stringBuilder = new StringBuilder(concatenatedData);
            foreach (var doc in documents)
            {
                stringBuilder.AppendFormat("-{0}-{1}-{2}-{3}", doc.FileType, doc.CreatedAt, doc.Size, doc.Base64Content);
            }
            concatenatedData = stringBuilder.ToString();


            newBlock.Hash = ComputeSha256Hash(concatenatedData); // Genera el hash y asigna al bloque
            await _context.SaveChangesAsync();

            return Ok("newBlock");
        }



        [HttpPost("mine/{blockId:int}/{numZeros:int}")]
        public async Task<IActionResult> MineBlock(int blockId, int numZeros)
        {
            // Obtener el bloque a minar
            var block = await _context.Blocks
                .Include(b => b.Documents)
                .FirstOrDefaultAsync(b => b.Id == blockId);

            if (block == null)
            {
                return NotFound("El bloque no existe.");
            }

            // Guardar el hash anterior del bloque
            string previousHash = block.Hash;

            // Crear variables para el proceso de minería
            int proof = 0;
            int milliseconds = 0;
            string requiredPrefix = new string('0', numZeros); // Cantidad de ceros requeridos

            // Usar `CancellationTokenSource` en un bloque `using`
            using (var cancellationTokenSource = new System.Threading.CancellationTokenSource())
            {
                var token = cancellationTokenSource.Token;
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    // Tarea para actualizar los milisegundos
                    var millisecondsTask = Task.Run(() =>
                    {
                        while (!token.IsCancellationRequested)
                        {
                            milliseconds = (int)stopwatch.ElapsedMilliseconds;
                            System.Threading.Thread.Sleep(1); // Espera de 1 milisegundo
                        }
                    }, token);

                    // Iniciar el proceso de minería
                    while (true)
                    {
                        DateTime minedAt = DateTime.UtcNow; // Actualizar el tiempo de minería
                        proof++; // Incrementar la prueba

                        // Crear el string concatenado para el hash usando StringBuilder
                        var stringBuilder = new StringBuilder();
                        stringBuilder.AppendFormat("{0}-{1}-{2}-{3}", minedAt, proof, milliseconds, block.PreviousHash);

                        // Agregar información de cada documento
                        foreach (var doc in block.Documents)
                        {
                            stringBuilder.AppendFormat("-{0}-{1}-{2}-{3}", doc.FileType, doc.CreatedAt, doc.Size, doc.Base64Content);
                        }

                        string concatenatedData = stringBuilder.ToString();

                        // Generar el hash usando SHA-256
                        string newHash = ComputeSha256Hash(concatenatedData);

                        // Verificar si el hash cumple con la cantidad de ceros requeridos
                        if (newHash.StartsWith(requiredPrefix))
                        {
                            // Actualizar los datos del bloque
                            block.Hash = newHash;
                            block.MinedAt = minedAt;
                            block.Proof = proof;
                            block.Milliseconds = milliseconds;
                            block.IsMined = true;

                            // Guardar el bloque actualizado
                            await _context.SaveChangesAsync();

                            // Buscar el bloque siguiente por el `PreviousHash`
                            var blockToUpdate = await _context.Blocks
                                .FirstOrDefaultAsync(b => b.PreviousHash == previousHash);

                            if (blockToUpdate != null)
                            {
                                blockToUpdate.PreviousHash = newHash;
                                await _context.SaveChangesAsync(); // Guardar los cambios
                            }

                            // Cancelar la tarea de milisegundos y finalizar
                            cancellationTokenSource.Cancel();
                            await millisecondsTask; // Esperar que la tarea finalice

                            return Ok(new
                            {
                                BlockId = blockId,
                                NewHash = newHash,
                                MinedAt = minedAt,
                                Proof = proof,
                                Milliseconds = milliseconds
                            });
                        }
                    }
                }
                finally
                {
                    // Asegurarse de detener el cronómetro y manejar tareas pendientes
                    stopwatch.Stop();
                }
            }
        }

        // Método para calcular el hash SHA-256
        private string ComputeSha256Hash(string rawData)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }

    }
}