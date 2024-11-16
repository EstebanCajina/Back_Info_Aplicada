namespace microserviceAuth.Models
{
    namespace microserviceAuth.Models
    {
        public class DocumentDto
        {
            public required string OwnerId { get; set; } // ID del propietario
            public required string FileType { get; set; }
            public required long Size { get; set; }
            public required string Base64Content { get; set; } // Contenido en Base64
        }
    }


}
