namespace microserviceAuth.Models
{
    namespace microserviceAuth.Models
    {
        public class SystemConfig
        {
            public int id {get; set; }

            // 1. MaxDocs: Maximum number of documents per block
            public  required int MaxDocs { get; set; }

            // 2. ProcessTime: Maximum allowed processing time for mining (in seconds)
            public required double ProcessTime { get; set; }

            // 3. QuantityOfZeros: Number of leading zeros required for the hash to meet complexity requirements
            public required int QuantityOfZeros { get; set; }
        }
    }
}
