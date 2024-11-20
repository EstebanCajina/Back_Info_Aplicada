using System.Threading.Tasks;

namespace microserviceAuth.Services
{
    public interface IAuditService
    {
        Task LogActionAsync(string description);
    }
}
