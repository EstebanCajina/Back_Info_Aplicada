namespace microserviceAuth.Models
    
{
    using global::microserviceAuth.Models.microserviceAuth.Models;
    public class Block
    {
        public int Id { get; set; }
        public DateTime? MinedAt { get; set; }
        public int? Proof { get; set; }
        public long? Milliseconds { get; set; }
        public required List<Document> Documents { get; set; }
        public required string PreviousHash { get; set; }
        public required string Hash { get; set; }
        public bool IsMined { get; set; } = false;

        // Nueva propiedad para guardar la cantidad de ceros iniciales requeridos
        public int LeadingZeros { get; set; }
    }
}
