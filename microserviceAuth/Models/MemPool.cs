namespace microserviceAuth.Models
{
    namespace microserviceAuth.Models
    {
        public class MemPool
        {
            public List<Document> Documents { get; set; } = new List<Document>();

            public void AddDocument(Document doc)
            {
                Documents.Add(doc);
            }

            public void Clear()
            {
                Documents.Clear(); // Limpia la MemPool después de minar
            }
        }
    }

}
