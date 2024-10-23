namespace microserviceAuth.Models
{
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using microserviceAuth.Models;

    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        // Agregar la tabla Documents
        public DbSet<Document> Documents { get; set; }
        public DbSet<SystemConfig> SystemConfigs { get; set; }
   

        public DbSet<Block> Blocks { get; set; }


    }

}
