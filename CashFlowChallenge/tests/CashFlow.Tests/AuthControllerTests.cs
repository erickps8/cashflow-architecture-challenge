using CashFlow.Auth.Api.Controllers;
using CashFlow.Auth.Api.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace CashFlow.Tests;

public class AuthControllerTests
{
    private readonly Mock<UserManager<IdentityUser>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<IConfiguration> _configurationMock;

    public AuthControllerTests()
    {
        var userStoreMock = new Mock<IUserStore<IdentityUser>>();

        _userManagerMock = new Mock<UserManager<IdentityUser>>(
            userStoreMock.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);

        var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();

        _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
            roleStoreMock.Object,
            null!,
            null!,
            null!,
            null!);

        _configurationMock = new Mock<IConfiguration>();

        _configurationMock.Setup(x => x["Jwt:Key"])
            .Returns("super-secret-key-super-secret-key");

        _configurationMock.Setup(x => x["Jwt:Issuer"])
            .Returns("cashflow");

        _configurationMock.Setup(x => x["Jwt:Audience"])
            .Returns("cashflow-users");
    }

    [Fact]
    public async Task Login_Should_Return_Unauthorized_When_User_Does_Not_Exist()
    {
        _userManagerMock
            .Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((IdentityUser?)null);

        var controller = new AuthController(
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _configurationMock.Object);

        var request = new LoginRequest
        {
            Username = "erick",
            Password = "123456"
        };

        var result = await controller.Login(request);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Register_Should_Return_Ok_When_User_Is_Created()
    {
        _userManagerMock
            .Setup(x => x.CreateAsync(
                It.IsAny<IdentityUser>(),
                It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _roleManagerMock
            .Setup(x => x.RoleExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(x => x.AddToRoleAsync(
                It.IsAny<IdentityUser>(),
                It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var controller = new AuthController(
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _configurationMock.Object);

        var request = new RegisterRequest
        {
            Username = "erick",
            Email = "erick@gmail.com",
            Password = "123456",
            Roles =
            [
                "Entry.Create"
            ]
        };

        var result = await controller.Register(request);

        result.Should().BeOfType<OkObjectResult>();

        _userManagerMock.Verify(x =>
            x.CreateAsync(
                It.IsAny<IdentityUser>(),
                "123456"),
            Times.Once);

        _userManagerMock.Verify(x =>
            x.AddToRoleAsync(
                It.IsAny<IdentityUser>(),
                "Entry.Create"),
            Times.Once);
    }

    [Fact]
    public async Task Login_Should_Return_Token_When_Credentials_Are_Valid()
    {
        var user = new IdentityUser
        {
            UserName = "erick"
        };

        _userManagerMock
            .Setup(x => x.FindByNameAsync("erick"))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(user, "123456"))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(["Entry.Create"]);

        var controller = new AuthController(
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _configurationMock.Object);

        var request = new LoginRequest
        {
            Username = "erick",
            Password = "123456"
        };

        var result = await controller.Login(request);

        result.Should().BeOfType<OkObjectResult>();
    }
}