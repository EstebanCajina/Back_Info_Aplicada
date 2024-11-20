using Xunit;
using Microsoft.EntityFrameworkCore;
using microserviceAuth.Controllers;
using microserviceAuth.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace microserviceAuth.Test
{
    public class AuditControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditController _controller;

        public AuditControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "AuditTestDB")
                .Options;

            _context = new ApplicationDbContext(options);
            _controller = new AuditController(_context);
        }

        [Fact]
        public async Task GetAllLogs_ReturnsNotFound_WhenNoLogsExist()
        {
            var result = await _controller.GetAllLogs();
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteAllLogs_ReturnsOk_WhenLogsAreDeleted()
        {
            _context.AuditLogs.Add(new AuditLog { Description = "Test log entry" });
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteAllLogs();
            Assert.IsType<OkObjectResult>(result);
        }
    }
}
