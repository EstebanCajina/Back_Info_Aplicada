namespace microserviceAuth.Models
{
    namespace microserviceAuth.Models
    {
        public class SystemConfigDto
        {
            // Maximum number of documents per block
            public int MaxDocs { get; set; }

            // Maximum allowed processing time for mining (in seconds)
            public double ProcessTime { get; set; }

            // Number of leading zeros required for the hash
            public int QuantityOfZeros { get; set; }
        }
    }
}