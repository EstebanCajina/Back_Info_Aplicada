using Xunit;
using Moq;
using System.Threading.Tasks;
using microserviceAuth.Controllers;
using microserviceAuth.Models;
using microserviceAuth.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using microserviceAuth.Models.microserviceAuth.Models;
using System.Text;

public class BlocksControllerTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly BlocksController _controller;

    public BlocksControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        _context = new ApplicationDbContext(options);

        _mockAuditService = new Mock<IAuditService>();
        _mockAuditService.Setup(audit => audit.LogActionAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        _controller = new BlocksController(_context, _mockAuditService.Object);
    }

    private void ResetDatabase()
    {
        _context.Blocks.RemoveRange(_context.Blocks);
        _context.Documents.RemoveRange(_context.Documents);
        _context.SaveChanges();
    }

    private string ComputeSha256Hash(string rawData)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }

    [Fact]
    public async Task GetAllBlocks_ReturnsOkResult_WhenBlocksExist()
    {
        ResetDatabase();
        _context.Blocks.AddRange(new List<Block>
        {
            new Block { Id = 1, Hash = "0001", PreviousHash = "0000", IsMined = true, Documents = new List<Document>() },
            new Block { Id = 2, Hash = "0002", PreviousHash = "0001", IsMined = true, Documents = new List<Document>() }
        });
        await _context.SaveChangesAsync();

        var result = await _controller.GetAllBlocks();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedBlocks = okResult.Value as IEnumerable<object>;
        Assert.NotNull(returnedBlocks);
        Assert.Equal(2, returnedBlocks.Count());
    }

    [Fact]
    public async Task GetAllBlocks_ReturnsNotFound_WhenNoBlocksExist()
    {
        ResetDatabase();

        var result = await _controller.GetAllBlocks();

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetLatestBlock_ReturnsOkResult_WithBlockDetails()
    {
        ResetDatabase();
        _context.Blocks.AddRange(new List<Block>
        {
            new Block { Id = 1, Hash = "0001", PreviousHash = "0000", IsMined = true, Documents = new List<Document>() },
            new Block { Id = 2, Hash = "0002", PreviousHash = "0001", IsMined = true, Documents = new List<Document>() }
        });
        await _context.SaveChangesAsync();

        var result = await _controller.GetLatestBlock();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var returnedBlock = Assert.IsType<Block>(okResult.Value);
        Assert.Equal(2, returnedBlock.Id);
    }

    [Fact]
    public async Task CreateAndMineBlock_ReturnsOkResult_WhenMiningSuccessful()
    {
        ResetDatabase();
        var lastBlock = new Block
        {
            Id = 1,
            Hash = "0001",
            PreviousHash = "0000",
            IsMined = true,
            Documents = new List<Document>()
        };
        _context.Blocks.Add(lastBlock);
        _context.Documents.Add(new Document
        {
            Id = 2,
            OwnerId = "user1",
            Size = 500,
            BlockId = null,
            FileType = "text/plain",
            CreatedAt = DateTime.Now,
            Base64Content = "dGVzdA=="
        });
        await _context.SaveChangesAsync();

        var result = await _controller.CreateAndMineBlock(1, 1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }


}
