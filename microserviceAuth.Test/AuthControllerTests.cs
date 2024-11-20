using Xunit;
using Moq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using microserviceAuth.Controllers;
using microserviceAuth.Models;
using microserviceAuth.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;

public class AuthControllerTests
{
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        var store = new Mock<IUserStore<User>>();
        _mockUserManager = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);

        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.SetupGet(x => x["JwtSettings:SecretKey"])
                          .Returns("ThisIsASuperSecretKeyForTestingPurposesOnly12345");

        _mockAuditService = new Mock<IAuditService>();
        _mockAuditService.Setup(audit => audit.LogActionAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        _controller = new AuthController(_mockUserManager.Object, _mockConfiguration.Object, _mockAuditService.Object);
    }

    [Fact]
    public async Task Register_ReturnsOkResult_WhenRegistrationIsSuccessful()
    {
        var registerDto = new RegisterDto
        {
            Username = "testuser",
            Email = "testuser@example.com",
            Password = "TestPassword123!",
            FirstName = "Test",
            LastName = "User",
            DateOfBirth = "01/01/2000"
        };

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                        .ReturnsAsync(IdentityResult.Success);

        var result = await _controller.Register(registerDto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Usuario registrado exitosamente", okResult.Value);
    }

    

}