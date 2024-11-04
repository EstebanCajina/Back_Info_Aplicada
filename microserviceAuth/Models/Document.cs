namespace microserviceAuth.Models
{
    namespace microserviceAuth.Models
    {
        public class Document
        {
            public int Id { get; set; }

            public string OwnerId { get; set; } // ID del propietario

            public string FileType { get; set; }
            public DateTime CreatedAt { get; set; }
            public long Size { get; set; } // Tamaño del archivo en bytes
            public string Base64Content { get; set; } // Contenido codificado en Base64

            public User Owner { get; set; }
        }
    }

}
