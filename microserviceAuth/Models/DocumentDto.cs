namespace microserviceAuth.Models
{
    namespace microserviceAuth.Models
    {
        public class DocumentDto
        {
            public string OwnerId { get; set; } // ID del propietario
            public string FileType { get; set; }
            public long Size { get; set; }
            public string Base64Content { get; set; } // Contenido en Base64
        }
    }


}
