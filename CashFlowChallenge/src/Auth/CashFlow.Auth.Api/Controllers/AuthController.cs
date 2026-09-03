using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CashFlow.Auth.Api.Data;
using CashFlow.Auth.Api.Models;
using CashFlow.Auth.Api.Services;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CashFlow.Auth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string DisplayNameClaim = "display_name";
    private const int MaxDisplayNameLength = 80;
    private static readonly Guid LegacyGroupId = new("11111111-1111-1111-1111-111111111111");
    private static readonly string[] DefaultFunctionalRoles = ["entries", "entries-create"];

    private readonly UserManager<IdentityUser> _users;
    private readonly AuthDbContext _db;
    private readonly IConfiguration _config;
    private readonly IPasswordResetNotifier _passwordResetNotifier;

    public AuthController(
        UserManager<IdentityUser> users,
        AuthDbContext db,
        IConfiguration config,
        IPasswordResetNotifier passwordResetNotifier)
    {
        _users = users;
        _db = db;
        _config = config;
        _passwordResetNotifier = passwordResetNotifier;
    }

    [HttpGet("groups/check")]
    public async Task<IActionResult> CheckGroup([FromQuery] string name)
    {
        var normalizedName = Normalize(name);
        if (string.IsNullOrWhiteSpace(normalizedName)) return BadRequest("Informe o nome do grupo.");

        var group = await _db.Groups.AsNoTracking()
            .FirstOrDefaultAsync(x => x.NormalizedName == normalizedName);

        return Ok(new { exists = group is not null, name = group?.Name ?? name.Trim() });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var displayName = NormalizeDisplayName(request.Name);
        if (string.IsNullOrWhiteSpace(displayName)) return BadRequest("Informe seu nome.");
        if (displayName.Length > MaxDisplayNameLength) return BadRequest($"O nome deve ter no máximo {MaxDisplayNameLength} caracteres.");
        if (string.IsNullOrWhiteSpace(request.GroupName)) return BadRequest("Informe o grupo.");
        if (string.IsNullOrWhiteSpace(request.Email)) return BadRequest("Informe o e-mail.");
        if (await _users.FindByEmailAsync(request.Email) is not null) return Conflict("E-mail já cadastrado.");

        var email = request.Email.Trim();
        var user = new IdentityUser { UserName = email, Email = email };
        var creation = await _users.CreateAsync(user, request.Password);
        if (!creation.Succeeded) return BadRequest(creation.Errors);

        try
        {
            await SetDisplayName(user, displayName);
            var membership = await JoinOrCreateGroup(user.Id, request.GroupName);
            return Ok(await State(
                user,
                membership,
                membership.Status == GroupMemberStatus.Active ? Token(user, membership) : PendingToken(user)));
        }
        catch
        {
            await _users.DeleteAsync(user);
            throw;
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _users.FindByNameAsync(request.Username)
                   ?? await _users.FindByEmailAsync(request.Username);

        if (user is null || !await _users.CheckPasswordAsync(user, request.Password))
            return Unauthorized();

        return Ok(await BuildLogin(user));
    }

    [HttpPost("password/forgot")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        const string message = "Se o e-mail estiver cadastrado, enviaremos as instruções para redefinir a senha.";
        if (string.IsNullOrWhiteSpace(request.Email)) return Ok(new { message });

        var user = await _users.FindByEmailAsync(request.Email.Trim());
        if (user is null) return Ok(new { message });

        var token = await _users.GeneratePasswordResetTokenAsync(user);
        await _passwordResetNotifier.SendAsync(user.Email!, token, cancellationToken);

        return Ok(new { message });
    }

    [HttpPost("password/reset")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Token)
            || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest("Dados para redefinição de senha inválidos.");

        var user = await _users.FindByEmailAsync(request.Email.Trim());
        if (user is null) return BadRequest("Link de redefinição inválido ou expirado.");

        var result = await _users.ResetPasswordAsync(user, request.Token, request.NewPassword);
        return result.Succeeded
            ? NoContent()
            : BadRequest(result.Errors.Select(x => x.Description));
    }

    [Authorize]
    [HttpPost("password/change")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var user = await CurrentUser();
        if (user is null) return Unauthorized();

        var result = await _users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        return result.Succeeded
            ? NoContent()
            : BadRequest(result.Errors.Select(x => x.Description));
    }

    [HttpPost("google")]
    public async Task<IActionResult> Google(GoogleLoginRequest request)
    {
        var clientId = _config["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId)) return StatusCode(503, "Login Google não configurado.");

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                request.IdToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = [clientId] });
        }
        catch
        {
            return Unauthorized("Token Google inválido.");
        }

        var user = await _users.FindByEmailAsync(payload.Email);
        var googleName = NormalizeDisplayName(payload.Name ?? string.Empty);
        if (googleName.Length > MaxDisplayNameLength) googleName = googleName[..MaxDisplayNameLength].Trim();

        if (user is null)
        {
            user = new IdentityUser { UserName = payload.Email, Email = payload.Email, EmailConfirmed = true };
            var creation = await _users.CreateAsync(user);
            if (!creation.Succeeded) return BadRequest(creation.Errors);
            if (!string.IsNullOrWhiteSpace(googleName)) await SetDisplayName(user, googleName);
            return Ok(await State(user, null, PendingToken(user)));
        }

        if (!string.IsNullOrWhiteSpace(googleName)) await SetDisplayName(user, googleName);
        return Ok(await BuildLogin(user));
    }

    [Authorize]
    [HttpPost("group")]
    public async Task<IActionResult> ChooseGroup(GroupChoiceRequest request)
    {
        var user = await CurrentUser();
        if (user is null) return Unauthorized();

        if (await _db.GroupMemberships.AnyAsync(x => x.UserId == user.Id && x.Status != GroupMemberStatus.Rejected))
            return Conflict("Usuário já possui grupo ou solicitação pendente.");

        var membership = await JoinOrCreateGroup(user.Id, request.GroupName);
        return Ok(await State(
            user,
            membership,
            membership.Status == GroupMemberStatus.Active ? Token(user, membership) : PendingToken(user)));
    }

    [Authorize]
    [HttpDelete("group/request")]
    public async Task<IActionResult> CancelPendingRequest()
    {
        var user = await CurrentUser();
        if (user is null) return Unauthorized();

        var pending = await _db.GroupMemberships
            .FirstOrDefaultAsync(x => x.UserId == user.Id && x.Status == GroupMemberStatus.Pending);
        if (pending is null) return NotFound("Nenhuma solicitação pendente encontrada.");

        _db.GroupMemberships.Remove(pending);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpGet("group")]
    public async Task<IActionResult> Group()
    {
        var membership = await CurrentMembership();
        if (membership is null || membership.Status != GroupMemberStatus.Active) return Forbid();

        var group = await _db.Groups.AsNoTracking().FirstAsync(x => x.Id == membership.GroupId);
        return Ok(new { id = group.Id, name = group.Name, role = membership.Role.ToString() });
    }

    [Authorize]
    [HttpPut("group")]
    public async Task<IActionResult> RenameGroup(GroupChoiceRequest request)
    {
        var membership = await CurrentMembership();
        if (membership is null || membership.Role != GroupMemberRole.Owner || membership.Status != GroupMemberStatus.Active)
            return Forbid();

        var normalizedName = Normalize(request.GroupName);
        if (string.IsNullOrWhiteSpace(normalizedName)) return BadRequest("Informe o nome do grupo.");

        var duplicated = await _db.Groups.AnyAsync(x => x.Id != membership.GroupId && x.NormalizedName == normalizedName);
        if (duplicated) return Conflict("Já existe um grupo com esse nome.");

        var group = await _db.Groups.FirstAsync(x => x.Id == membership.GroupId);
        group.Name = request.GroupName.Trim();
        group.NormalizedName = normalizedName;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpGet("group/members")]
    public async Task<IActionResult> Members()
    {
        var currentMembership = await CurrentMembership();
        if (currentMembership is null || currentMembership.Status != GroupMemberStatus.Active) return Forbid();

        var memberships = await _db.GroupMemberships
            .Where(x => x.GroupId == currentMembership.GroupId)
            .OrderBy(x => x.Status)
            .ToListAsync();

        var result = new List<object>();
        foreach (var membership in memberships)
        {
            var user = await _users.FindByIdAsync(membership.UserId);
            result.Add(new
            {
                id = membership.Id,
                name = user is null ? null : await GetDisplayName(user),
                email = user?.Email,
                username = user?.UserName,
                status = membership.Status.ToString(),
                role = membership.Role.ToString()
            });
        }

        return Ok(result);
    }

    [Authorize]
    [HttpPut("group/members/{id:guid}")]
    public async Task<IActionResult> Decide(Guid id, MembershipDecisionRequest request)
    {
        var currentMembership = await CurrentMembership();
        if (currentMembership is null
            || currentMembership.Role != GroupMemberRole.Owner
            || currentMembership.Status != GroupMemberStatus.Active)
            return Forbid();

        var target = await _db.GroupMemberships
            .FirstOrDefaultAsync(x => x.Id == id && x.GroupId == currentMembership.GroupId);
        if (target is null) return NotFound();
        if (target.Role == GroupMemberRole.Owner) return BadRequest();

        target.Status = request.Approve ? GroupMemberStatus.Active : GroupMemberStatus.Rejected;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpDelete("group/members/{id:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id)
    {
        var currentMembership = await CurrentMembership();
        if (currentMembership is null
            || currentMembership.Role != GroupMemberRole.Owner
            || currentMembership.Status != GroupMemberStatus.Active)
            return Forbid();

        var target = await _db.GroupMemberships
            .FirstOrDefaultAsync(x => x.Id == id && x.GroupId == currentMembership.GroupId);
        if (target is null) return NotFound();
        if (target.Role == GroupMemberRole.Owner) return BadRequest("O gestor não pode ser removido do próprio grupo.");

        _db.GroupMemberships.Remove(target);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<object> BuildLogin(IdentityUser user)
    {
        var membership = await _db.GroupMemberships.Include(x => x.Group)
            .FirstOrDefaultAsync(x => x.UserId == user.Id && x.Status != GroupMemberStatus.Rejected);

        if (membership?.GroupId == LegacyGroupId)
            membership = await MoveLegacyNamedGroupToFreshTenant(membership.Group, user.Id);

        return await State(
            user,
            membership,
            membership?.Status == GroupMemberStatus.Active
                ? Token(user, membership)
                : PendingToken(user));
    }

    private async Task<object> State(IdentityUser user, GroupMembership? membership, string? token) => new
    {
        token,
        name = await GetDisplayName(user),
        username = user.UserName,
        email = user.Email,
        requiresGroup = membership is null,
        pendingApproval = membership?.Status == GroupMemberStatus.Pending,
        group = membership is null ? null : new
        {
            id = membership.GroupId,
            name = membership.Group.Name,
            role = membership.Role.ToString()
        },
        message = membership?.Status == GroupMemberStatus.Pending
            ? "Solicitação enviada ao gestor do grupo."
            : null
    };

    private async Task<GroupMembership> JoinOrCreateGroup(string userId, string name)
    {
        var normalizedName = Normalize(name);
        var group = await _db.Groups.Include(x => x.Memberships)
            .FirstOrDefaultAsync(x => x.NormalizedName == normalizedName);

        if (group is null)
        {
            group = new CashFlowGroup
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                NormalizedName = normalizedName,
                OwnerUserId = userId
            };
            var owner = new GroupMembership
            {
                Group = group,
                UserId = userId,
                Role = GroupMemberRole.Owner,
                Status = GroupMemberStatus.Active
            };
            _db.AddRange(group, owner);
            await _db.SaveChangesAsync();
            return owner;
        }

        if (group.Id == LegacyGroupId) return await MoveLegacyNamedGroupToFreshTenant(group, userId);

        var pending = new GroupMembership
        {
            GroupId = group.Id,
            Group = group,
            UserId = userId,
            Role = GroupMemberRole.Member,
            Status = GroupMemberStatus.Pending
        };
        _db.Add(pending);
        await _db.SaveChangesAsync();
        return pending;
    }

    private async Task<GroupMembership> MoveLegacyNamedGroupToFreshTenant(CashFlowGroup legacy, string userId)
    {
        var oldName = legacy.Name;
        var oldNormalizedName = legacy.NormalizedName;
        legacy.Name = "Dados anteriores";
        legacy.NormalizedName = $"__LEGACY_{LegacyGroupId:N}";
        await _db.SaveChangesAsync();

        var fresh = new CashFlowGroup
        {
            Id = Guid.NewGuid(),
            Name = oldName,
            NormalizedName = oldNormalizedName,
            OwnerUserId = legacy.OwnerUserId
        };
        _db.Groups.Add(fresh);
        await _db.SaveChangesAsync();

        var memberships = await _db.GroupMemberships.Where(x => x.GroupId == LegacyGroupId).ToListAsync();
        foreach (var membership in memberships) membership.GroupId = fresh.Id;
        await _db.SaveChangesAsync();

        var existing = await _db.GroupMemberships.Include(x => x.Group)
            .FirstOrDefaultAsync(x =>
                x.UserId == userId
                && x.GroupId == fresh.Id
                && x.Status != GroupMemberStatus.Rejected);
        if (existing is not null) return existing;

        var pending = new GroupMembership
        {
            GroupId = fresh.Id,
            UserId = userId,
            Role = GroupMemberRole.Member,
            Status = GroupMemberStatus.Pending
        };
        _db.Add(pending);
        await _db.SaveChangesAsync();
        return await _db.GroupMemberships.Include(x => x.Group).FirstAsync(x => x.Id == pending.Id);
    }

    private string Token(IdentityUser user, GroupMembership membership)
    {
        var claims = BaseClaims(user);

        foreach (var role in DefaultFunctionalRoles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        claims.Add(new Claim("group_id", membership.GroupId.ToString()));
        claims.Add(new Claim("group_role", membership.Role.ToString()));

        return WriteToken(claims, 8);
    }

    private string PendingToken(IdentityUser user) => WriteToken(BaseClaims(user), 1);

    private List<Claim> BaseClaims(IdentityUser user) =>
    [
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Name, user.UserName!),
        new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
    ];

    private async Task<string> GetDisplayName(IdentityUser user)
    {
        var claims = await _users.GetClaimsAsync(user);
        var stored = claims.FirstOrDefault(x => x.Type == DisplayNameClaim)?.Value;
        if (!string.IsNullOrWhiteSpace(stored)) return stored;

        var fallback = user.Email?.Split('@', 2)[0] ?? user.UserName ?? "Usuário";
        return string.IsNullOrWhiteSpace(fallback) ? "Usuário" : fallback;
    }

    private async Task SetDisplayName(IdentityUser user, string name)
    {
        var normalized = NormalizeDisplayName(name);
        if (string.IsNullOrWhiteSpace(normalized)) return;

        var claims = await _users.GetClaimsAsync(user);
        var current = claims.FirstOrDefault(x => x.Type == DisplayNameClaim);
        if (current?.Value == normalized) return;

        if (current is not null)
        {
            var removal = await _users.RemoveClaimAsync(user, current);
            if (!removal.Succeeded) throw new InvalidOperationException("Não foi possível atualizar o nome do usuário.");
        }

        var addition = await _users.AddClaimAsync(user, new Claim(DisplayNameClaim, normalized));
        if (!addition.Succeeded) throw new InvalidOperationException("Não foi possível salvar o nome do usuário.");
    }

    private string WriteToken(IEnumerable<Claim> claims, int hours)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var jwt = new JwtSecurityToken(
            _config["Jwt:Issuer"],
            _config["Jwt:Audience"],
            claims,
            expires: DateTime.UtcNow.AddHours(hours),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private Task<IdentityUser?> CurrentUser() => _users.GetUserAsync(User);

    private async Task<GroupMembership?> CurrentMembership()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId is null
            ? null
            : await _db.GroupMemberships.FirstOrDefaultAsync(x => x.UserId == userId && x.Status != GroupMemberStatus.Rejected);
    }

    private static string Normalize(string name) => name.Trim().ToUpperInvariant();

    private static string NormalizeDisplayName(string name) =>
        string.Join(' ', name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}