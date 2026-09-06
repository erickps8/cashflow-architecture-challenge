using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CashFlow.Auth.Api.Data;
using CashFlow.Auth.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CashFlow.Auth.Api.Controllers;

[ApiController]
[Route("api/auth/session")]
public sealed class SessionController : ControllerBase
{
    private const string LoginProvider = "CashFlowRefresh";
    private const int RefreshLifetimeDays = 30;
    private static readonly string[] DefaultFunctionalRoles = ["entries", "entries-create"];

    private readonly UserManager<IdentityUser> _users;
    private readonly AuthDbContext _db;
    private readonly IConfiguration _config;

    public SessionController(UserManager<IdentityUser> users, AuthDbContext db, IConfiguration config)
    {
        _users = users;
        _db = db;
        _config = config;
    }

    [Authorize]
    [HttpPost("start")]
    public async Task<IActionResult> Start()
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var sessionId = Guid.NewGuid().ToString("N");
        var refreshToken = CreateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddDays(RefreshLifetimeDays);
        var storedValue = SerializeStoredToken(expiresAt, Hash(refreshToken));

        var result = await _users.SetAuthenticationTokenAsync(user, LoginProvider, sessionId, storedValue);
        if (!result.Succeeded) return StatusCode(500, "Não foi possível iniciar a sessão.");

        return Ok(new RefreshSessionResponse(user.Id, sessionId, refreshToken, expiresAt, null));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshSessionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId)
            || string.IsNullOrWhiteSpace(request.SessionId)
            || string.IsNullOrWhiteSpace(request.RefreshToken))
            return Unauthorized();

        var user = await _users.FindByIdAsync(request.UserId);
        if (user is null) return Unauthorized();

        var storedValue = await _users.GetAuthenticationTokenAsync(user, LoginProvider, request.SessionId);
        if (!TryParseStoredToken(storedValue, out var expiresAt, out var storedHash)
            || expiresAt <= DateTime.UtcNow
            || !FixedTimeEquals(storedHash, Hash(request.RefreshToken)))
        {
            await _users.RemoveAuthenticationTokenAsync(user, LoginProvider, request.SessionId);
            return Unauthorized();
        }

        var membership = await _db.GroupMemberships
            .Include(x => x.Group)
            .FirstOrDefaultAsync(x => x.UserId == user.Id && x.Status != GroupMemberStatus.Rejected);

        var accessToken = membership?.Status == GroupMemberStatus.Active
            ? CreateAccessToken(user, membership, 8)
            : CreateAccessToken(user, null, 1);

        var rotatedRefreshToken = CreateRefreshToken();
        var rotatedExpiresAt = DateTime.UtcNow.AddDays(RefreshLifetimeDays);
        var rotatedValue = SerializeStoredToken(rotatedExpiresAt, Hash(rotatedRefreshToken));
        var rotation = await _users.SetAuthenticationTokenAsync(user, LoginProvider, request.SessionId, rotatedValue);
        if (!rotation.Succeeded) return StatusCode(500, "Não foi possível renovar a sessão.");

        return Ok(new RefreshSessionResponse(user.Id, request.SessionId, rotatedRefreshToken, rotatedExpiresAt, accessToken));
    }

    [Authorize]
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke(RevokeSessionRequest request)
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.SessionId)) return NoContent();

        await _users.RemoveAuthenticationTokenAsync(user, LoginProvider, request.SessionId);
        return NoContent();
    }

    private string CreateAccessToken(IdentityUser user, GroupMembership? membership, int hours)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        if (membership is not null)
        {
            foreach (var role in DefaultFunctionalRoles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            claims.Add(new Claim("group_id", membership.GroupId.ToString()));
            claims.Add(new Claim("group_role", membership.Role.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var jwt = new JwtSecurityToken(
            _config["Jwt:Issuer"],
            _config["Jwt:Audience"],
            claims,
            expires: DateTime.UtcNow.AddHours(hours),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static string CreateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string SerializeStoredToken(DateTime expiresAt, string hash) => $"{expiresAt.Ticks}:{hash}";

    private static bool TryParseStoredToken(string? value, out DateTime expiresAt, out string hash)
    {
        expiresAt = default;
        hash = string.Empty;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var separator = value.IndexOf(':');
        if (separator <= 0 || !long.TryParse(value[..separator], out var ticks)) return false;

        try
        {
            expiresAt = new DateTime(ticks, DateTimeKind.Utc);
            hash = value[(separator + 1)..];
            return !string.IsNullOrWhiteSpace(hash);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}

public sealed record RefreshSessionRequest(string UserId, string SessionId, string RefreshToken);
public sealed record RevokeSessionRequest(string SessionId);
public sealed record RefreshSessionResponse(string UserId, string SessionId, string RefreshToken, DateTime ExpiresAt, string? Token);
