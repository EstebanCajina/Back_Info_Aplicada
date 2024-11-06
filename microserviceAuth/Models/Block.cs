namespace microserviceAuth.Models
{
    namespace microserviceAuth.Models
    {

        public class Block
        {
            // 1. Id: Block number, starting from 1
            public int Id { get; set; }

            // 2. MinedAt: Date and time the block was mined in format YYYYMMDDTHHMMSS
            public DateTime? MinedAt { get; set; } // Stored as DateTime, can format it during mining

            // 3. Proof: A 32-bit integer defined during the mining process
            public int Proof { get; set; }

            // 4. Milliseconds: Time in milliseconds used to mine the block
            public long Milliseconds { get; set; }

            // 5. Documents: List of documents included in the block (limit max transactions per block)
            public List<Document> Documents { get; set; }

            // 6. PreviousHash: Hash (SHA-256) of the previous block in the chain, 64 zeros for the first block
            public string PreviousHash { get; set; }

            // 7. Hash: Hash of the entire block's data, must meet complexity requirements (e.g., first 4 digits should be zeros)
            public string Hash { get; set; }

            public bool IsMined { get; set; } = false;


        }
    }
}