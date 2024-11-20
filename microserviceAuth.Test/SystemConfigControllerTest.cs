using Xunit;
using Moq;
using System.Threading.Tasks;
using microserviceAuth.Controllers;
using microserviceAuth.Models;
using microserviceAuth.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using microserviceAuth.Models.microserviceAuth.Models;
using System.Linq;

public class SystemConfigControllerTests
{
    private readonly ApplicationDbContext _context;
    private readonly SystemConfigController _controller;
    private readonly Mock<IAuditService> _mockAuditService;

    public SystemConfigControllerTests()
    {
        // Configuración de la base de datos en memoria
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        _context = new ApplicationDbContext(options);

        // Configuración del servicio de auditoría
        _mockAuditService = new Mock<IAuditService>();
        _mockAuditService.Setup(audit => audit.LogActionAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        _controller = new SystemConfigController(_context, _mockAuditService.Object);
    }

    private void ResetDatabase()
    {
        _context.SystemConfigs.RemoveRange(_context.SystemConfigs);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetSystemConfig_ReturnsOkResult_WithSystemConfig()
    {
        ResetDatabase();

        // Agregar una configuración de sistema a la base de datos
        var config = new SystemConfig { MaxDocs = 10, ProcessTime = 120, QuantityOfZeros = 4 };
        _context.SystemConfigs.Add(config);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSystemConfig();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedConfig = okResult.Value as SystemConfig;
        Assert.NotNull(returnedConfig);
        Assert.Equal(10, returnedConfig.MaxDocs);
        Assert.Equal(120, returnedConfig.ProcessTime);
        Assert.Equal(4, returnedConfig.QuantityOfZeros);
    }

    [Fact]
    public async Task GetSystemConfig_ReturnsNotFound_WhenConfigDoesNotExist()
    {
        ResetDatabase();

        // Act
        var result = await _controller.GetSystemConfig();

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("No se encontró la configuración del sistema.", notFoundResult.Value);
    }

    [Fact]
    public async Task AddSystemConfig_ReturnsOkResult_WhenConfigIsAdded()
    {
        ResetDatabase();

        var configDto = new SystemConfigDto { MaxDocs = 10, ProcessTime = 120, QuantityOfZeros = 4 };

        // Act
        var result = await _controller.AddSystemConfig(configDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Configuración del sistema añadida exitosamente.", okResult.Value);

        // Verificar que la configuración se haya guardado en la base de datos
        var config = _context.SystemConfigs.FirstOrDefault();
        Assert.NotNull(config);
        Assert.Equal(10, config.MaxDocs);
        Assert.Equal(120, config.ProcessTime);
        Assert.Equal(4, config.QuantityOfZeros);
    }

    [Fact]
    public async Task AddSystemConfig_ReturnsBadRequest_WhenConfigAlreadyExists()
    {
        ResetDatabase();

        // Agregar una configuración de sistema inicial
        _context.SystemConfigs.Add(new SystemConfig { MaxDocs = 10, ProcessTime = 120, QuantityOfZeros = 4 });
        await _context.SaveChangesAsync();

        var configDto = new SystemConfigDto { MaxDocs = 15, ProcessTime = 180, QuantityOfZeros = 5 };

        // Act
        var result = await _controller.AddSystemConfig(configDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("La configuración ya existe. Use el método de edición para actualizarla.", badRequestResult.Value);
    }

    [Fact]
    public async Task EditSystemConfig_ReturnsOkResult_WhenConfigIsUpdated()
    {
        ResetDatabase();

        // Agregar una configuración de sistema inicial
        var config = new SystemConfig { MaxDocs = 10, ProcessTime = 120, QuantityOfZeros = 4 };
        _context.SystemConfigs.Add(config);
        await _context.SaveChangesAsync();

        var updatedConfigDto = new SystemConfigDto { MaxDocs = 15, ProcessTime = 180, QuantityOfZeros = 5 };

        // Act
        var result = await _controller.EditSystemConfig(updatedConfigDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Configuración del sistema actualizada exitosamente.", okResult.Value);

        // Verificar que la configuración se haya actualizado en la base de datos
        var updatedConfig = _context.SystemConfigs.FirstOrDefault();
        Assert.NotNull(updatedConfig);
        Assert.Equal(15, updatedConfig.MaxDocs);
        Assert.Equal(180, updatedConfig.ProcessTime);
        Assert.Equal(5, updatedConfig.QuantityOfZeros);
    }

    [Fact]
    public async Task EditSystemConfig_ReturnsNotFound_WhenConfigDoesNotExist()
    {
        ResetDatabase();

        var configDto = new SystemConfigDto { MaxDocs = 15, ProcessTime = 180, QuantityOfZeros = 5 };

        // Act
        var result = await _controller.EditSystemConfig(configDto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("No se encontró la configuración para actualizar.", notFoundResult.Value);
    }
}
