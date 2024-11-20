using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using microserviceAuth.Services;
using microserviceAuth.Models;

public class AuditServiceTests
{
    private readonly AuditService _auditService;
    private readonly ApplicationDbContext _context;

    public AuditServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "AuditServiceTestDb")
            .Options;
        _context = new ApplicationDbContext(options);
        _auditService = new AuditService(_context);
    }

    [Fact]
    public async Task LogActionAsync_SavesAuditLogSuccessfully()
    {
        // Arrange
        var description = "Test action";

        // Act
        await _auditService.LogActionAsync(description);

        // Assert
        var logs = await _context.AuditLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal(description, logs[0].Description);
    }
}
