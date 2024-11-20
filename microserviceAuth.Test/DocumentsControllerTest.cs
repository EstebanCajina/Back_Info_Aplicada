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
using System;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Collections;
using microserviceAuth.Encrypted;

public class DocumentsControllerTests
{
    private readonly ApplicationDbContext _context;
    private readonly DocumentsController _controller;
    private readonly Mock<IAuditService> _mockAuditService;

    public DocumentsControllerTests()
    {
        // Configuración de la base de datos en memoria
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        _context = new ApplicationDbContext(options);

        // Configuración del servicio de auditoría
        _mockAuditService = new Mock<IAuditService>();
        _mockAuditService.Setup(audit => audit.LogActionAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Configuración de AesEncryption con ConfigurationBuilder
        var inMemorySettings = new Dictionary<string, string> {
            {"EncryptionSettings:Key", "your-32-character-256-bit-secret"},
            {"EncryptionSettings:Iv", "your-128-bit-IV-"}
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var encryption = new AesEncryption(configuration);

        _controller = new DocumentsController(_context, encryption, _mockAuditService.Object);
    }

    private void ResetDatabase()
    {
        _context.Documents.RemoveRange(_context.Documents);
        _context.SaveChanges();
    }

    [Fact]
    public async Task UploadDocument_ReturnsOkResult_WhenDocumentIsUploaded()
    {
        ResetDatabase();

        var documentDto = new DocumentDto
        {
            OwnerId = "user1",
            FileType = "text/plain",
            Size = 500,
            Base64Content = "dGVzdA=="
        };

        var result = await _controller.UploadDocument(documentDto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Documento subido y guardado en la base de datos.", okResult.Value);

        var document = _context.Documents.FirstOrDefault(d => d.OwnerId == "user1");
        Assert.NotNull(document);
        Assert.Equal("text/plain", document.FileType);
    }

  

    

    [Fact]
    public async Task DeleteDocument_ReturnsOkResult_WhenDocumentIsDeleted()
    {
        ResetDatabase();

        var document = new Document
        {
            Id = 1,
            OwnerId = "user1",
            FileType = "text/plain",
            CreatedAt = DateTime.Now,
            Size = 500,
            Base64Content = "encryptedContent"
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        var result = await _controller.DeleteDocument(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Documento eliminado exitosamente.", okResult.Value);

        var deletedDocument = _context.Documents.Find(1);
        Assert.Null(deletedDocument);
    }

    [Fact]
    public async Task DeleteDocument_ReturnsNotFound_WhenDocumentDoesNotExist()
    {
        ResetDatabase();

        var result = await _controller.DeleteDocument(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteMultipleDocuments_ReturnsOkResult_WhenDocumentsAreDeleted()
    {
        ResetDatabase();

        var documents = new List<Document>
        {
            new Document { Id = 1, OwnerId = "user1", FileType = "text/plain", CreatedAt = DateTime.Now, Size = 500, Base64Content = "encryptedContent" },
            new Document { Id = 2, OwnerId = "user1", FileType = "text/plain", CreatedAt = DateTime.Now, Size = 500, Base64Content = "encryptedContent" }
        };
        _context.Documents.AddRange(documents);
        await _context.SaveChangesAsync();

        var idsToDelete = new List<int> { 1, 2 };
        var result = await _controller.DeleteMultipleDocuments(idsToDelete);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Documentos eliminados exitosamente.", okResult.Value);

        var remainingDocuments = _context.Documents.Where(d => idsToDelete.Contains(d.Id)).ToList();
        Assert.Empty(remainingDocuments);
    }

    [Fact]
    public async Task DeleteMultipleDocuments_ReturnsNotFound_WhenDocumentsDoNotExist()
    {
        ResetDatabase();

        var idsToDelete = new List<int> { 999, 1000 };
        var result = await _controller.DeleteMultipleDocuments(idsToDelete);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    

    

    [Fact]
    public async Task GetDocumentById_ReturnsNotFound_WhenDocumentDoesNotExist()
    {
        ResetDatabase();

        var result = await _controller.GetDocumentById(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
