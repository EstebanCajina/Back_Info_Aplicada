namespace microserviceAuth.Models
{
    namespace microserviceAuth.Models
    {
        public class Document
        {
            public int Id { get; set; }
            public required string OwnerId { get; set; }
            public required string FileType { get; set; }
            public required DateTime CreatedAt { get; set; }
            public required long Size { get; set; }
            public required string Base64Content { get; set; }

            public int? BlockId { get; set; } // ID del bloque al que pertenece el documento
            public Block Block { get; set; } // Relación con el bloque

            public User Owner { get; set; }
        }
    }
}
