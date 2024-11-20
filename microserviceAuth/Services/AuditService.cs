using microserviceAuth.Models;
using System.Threading.Tasks;

namespace microserviceAuth.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogActionAsync(string description)
        {
            var auditLog = new AuditLog { Description = description };
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
    }
}
