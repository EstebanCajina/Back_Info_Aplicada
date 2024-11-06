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

        if (blocks == null || !blocks.Any())
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
            IsMined=false
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
        DateTime minedAt = DateTime.Now;
        int proof = 0;
        int milliseconds = 0;

        // Crear la variable para la cantidad de ceros
        string requiredPrefix = new string('0', numZeros);

        // Empezamos el hilo para contar los milisegundos
        var cancellationTokenSource = new System.Threading.CancellationTokenSource();
        var token = cancellationTokenSource.Token;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Hilo para actualizar los milisegundos
        var millisecondsTask = Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                milliseconds = (int)stopwatch.ElapsedMilliseconds;
                System.Threading.Thread.Sleep(1); // Espera de 1 milisegundo
            }
        });

        // Iniciar el proceso de minería
        while (true)
        {
            minedAt = DateTime.Now; // Actualizar el tiempo de minería
            proof++; // Incrementar la prueba

            // Crear el string concatenado para el hash
            string concatenatedData = $"{minedAt}-{proof}-{milliseconds}-{block.PreviousHash}";
            foreach (var doc in block.Documents)
            {
                concatenatedData += $"-{doc.FileType}-{doc.CreatedAt}-{doc.Size}-{doc.Base64Content}";
            }

            // Generar el hash usando SHA-256
            string newHash = ComputeSha256Hash(concatenatedData);

            // Verificar si el hash cumple con la cantidad de ceros requeridos
            if (newHash.StartsWith(requiredPrefix))
            {
                // Si el hash es válido, se actualizan los datos del bloque
                block.Hash = newHash;
                block.MinedAt = minedAt;
                block.Proof = proof;
                block.Milliseconds = milliseconds;
                block.IsMined = true;

                // Guardamos el bloque actualizado
                await _context.SaveChangesAsync();

                // Buscar el bloque con el PreviousHash igual al hash del bloque actual
                var blockToUpdate = await _context.Blocks
                    .FirstOrDefaultAsync(b => b.PreviousHash == previousHash);

                if (blockToUpdate != null)
                {
                    // Actualizar el PreviousHash del bloque encontrado con el nuevo hash
                    blockToUpdate.PreviousHash = newHash;

                    // Guardar los cambios en el bloque encontrado
                    await _context.SaveChangesAsync();
                }

                // Ahora ya no se crea un nuevo bloque, sino que se actualiza el existente
                cancellationTokenSource.Cancel();
                millisecondsTask.Wait();

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
