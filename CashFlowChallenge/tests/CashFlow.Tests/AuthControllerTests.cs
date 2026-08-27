using CashFlow.Auth.Api.Controllers;
using CashFlow.Auth.Api.Data;
using CashFlow.Auth.Api.Models;
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
    private readonly IConfiguration _configuration;

    public AuthControllerTests()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        _users = new Mock<UserManager<IdentityUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
        {
            ["Jwt:Key"] = "super-secret-key-super-secret-key",
            ["Jwt:Issuer"] = "cashflow",
            ["Jwt:Audience"] = "cashflow-users"
        }).Build();
    }

    private AuthDbContext Db(string name) => new(new DbContextOptionsBuilder<AuthDbContext>().UseInMemoryDatabase(name).Options);

    [Fact]
    public async Task Login_Should_Return_Unauthorized_When_User_Does_Not_Exist()
    {
        _users.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);
        _users.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);
        await using var db = Db(Guid.NewGuid().ToString());
        var controller = new AuthController(_users.Object, db, _configuration);

        var result = await controller.Login(new LoginRequest { Username = "nobody@test.local", Password = "123456" });

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Register_First_User_Should_Create_New_Group_As_Active_Owner()
    {
        _users.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);
        _users.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        await using var db = Db(Guid.NewGuid().ToString());
        var controller = new AuthController(_users.Object, db, _configuration);

        var result = await controller.Register(new RegisterRequest { Username = "Owner", Email = "owner@test.local", Password = "123456", GroupName = "Familia Teste" });

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
        var group = new CashFlowGroup { Name = "Familia Teste", NormalizedName = "FAMILIA TESTE", OwnerUserId = "owner" };
        await using var db = Db(Guid.NewGuid().ToString());
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        _users.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);
        _users.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        var controller = new AuthController(_users.Object, db, _configuration);

        var result = await controller.Register(new RegisterRequest { Username = "Member", Email = "member@test.local", Password = "123456", GroupName = "Familia Teste" });

        result.Should().BeOfType<OkObjectResult>();
        var membership = await db.GroupMemberships.SingleAsync();
        membership.GroupId.Should().Be(group.Id);
        membership.Role.Should().Be(GroupMemberRole.Member);
        membership.Status.Should().Be(GroupMemberStatus.Pending);
    }
}
