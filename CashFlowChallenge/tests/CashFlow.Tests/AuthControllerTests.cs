using CashFlow.Auth.Api.Controllers;
using CashFlow.Auth.Api.Data;
using CashFlow.Auth.Api.Models;
using CashFlow.Auth.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace CashFlow.Tests;

public class AuthControllerTests
{
    private readonly Mock<UserManager<IdentityUser>> _users;
    private readonly Mock<IPasswordResetNotifier> _passwordResetNotifier = new();
    private readonly IConfiguration _configuration;

    public AuthControllerTests()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        _users = new Mock<UserManager<IdentityUser>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "super-secret-key-super-secret-key",
                ["Jwt:Issuer"] = "cashflow",
                ["Jwt:Audience"] = "cashflow-users"
            })
            .Build();
    }

    private AuthDbContext Db(string name) =>
        new(new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(name)
            .Options);

    private AuthController CreateController(AuthDbContext db) =>
        new(_users.Object, db, _configuration, _passwordResetNotifier.Object);

    [Fact]
    public async Task Login_Should_Return_Unauthorized_When_User_Does_Not_Exist()
    {
        _users.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((IdentityUser?)null);
        _users.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((IdentityUser?)null);

        await using var db = Db(Guid.NewGuid().ToString());
        var controller = CreateController(db);

        var result = await controller.Login(new LoginRequest
        {
            Username = "nobody@test.local",
            Password = "123456"
        });

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Register_First_User_Should_Create_New_Group_As_Active_Owner()
    {
        _users.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((IdentityUser?)null);
        _users.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        await using var db = Db(Guid.NewGuid().ToString());
        var controller = CreateController(db);

        var result = await controller.Register(new RegisterRequest
        {
            Username = "Owner",
            Email = "owner@test.local",
            Password = "123456",
            GroupName = "Familia Teste"
        });

        result.Should().BeOfType<OkObjectResult>();

        var group = await db.Groups.SingleAsync();
        var membership = await db.GroupMemberships.SingleAsync();

        group.Id.Should().NotBe(Guid.Empty);
        membership.GroupId.Should().Be(group.Id);
        membership.Role.Should().Be(GroupMemberRole.Owner);
        membership.Status.Should().Be(GroupMemberStatus.Active);
    }

    [Fact]
    public async Task Register_Second_User_In_Existing_Group_Should_Be_Pending_Member()
    {
        var group = new CashFlowGroup
        {
            Name = "Familia Teste",
            NormalizedName = "FAMILIA TESTE",
            OwnerUserId = "owner"
        };

        await using var db = Db(Guid.NewGuid().ToString());
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        _users.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((IdentityUser?)null);
        _users.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var controller = CreateController(db);

        var result = await controller.Register(new RegisterRequest
        {
            Username = "Member",
            Email = "member@test.local",
            Password = "123456",
            GroupName = "Familia Teste"
        });

        result.Should().BeOfType<OkObjectResult>();

        var membership = await db.GroupMemberships.SingleAsync();
        membership.GroupId.Should().Be(group.Id);
        membership.Role.Should().Be(GroupMemberRole.Member);
        membership.Status.Should().Be(GroupMemberStatus.Pending);
    }

    [Fact]
    public async Task ForgotPassword_Should_Not_Reveal_When_Email_Does_Not_Exist()
    {
        _users.Setup(x => x.FindByEmailAsync("missing@test.local"))
            .ReturnsAsync((IdentityUser?)null);

        await using var db = Db(Guid.NewGuid().ToString());
        var controller = CreateController(db);

        var result = await controller.ForgotPassword(
            new ForgotPasswordRequest("missing@test.local"),
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _passwordResetNotifier.Verify(
            x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ForgotPassword_Should_Send_Reset_Token_When_Email_Exists()
    {
        var user = new IdentityUser
        {
            Id = "user-1",
            Email = "user@test.local",
            UserName = "user@test.local"
        };

        _users.Setup(x => x.FindByEmailAsync(user.Email))
            .ReturnsAsync(user);
        _users.Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync("reset-token");

        await using var db = Db(Guid.NewGuid().ToString());
        var controller = CreateController(db);

        var result = await controller.ForgotPassword(
            new ForgotPasswordRequest(user.Email),
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _passwordResetNotifier.Verify(
            x => x.SendAsync(user.Email, "reset-token", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
