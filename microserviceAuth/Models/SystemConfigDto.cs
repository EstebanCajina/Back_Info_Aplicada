namespace microserviceAuth.Models
{
    namespace microserviceAuth.Models
    {
        public class SystemConfigDto
        {
            // Maximum number of documents per block
            public required int MaxDocs { get; set; }

            // Maximum allowed processing time for mining (in seconds)
            public required double ProcessTime { get; set; }

            // Number of leading zeros required for the hash
            public required int QuantityOfZeros { get; set; }
        }
    }
}