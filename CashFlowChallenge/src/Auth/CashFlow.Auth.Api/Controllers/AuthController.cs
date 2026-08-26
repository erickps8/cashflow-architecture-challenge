using CashFlow.Auth.Api.Data;
using CashFlow.Auth.Api.Models;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CashFlow.Auth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _users;
    private readonly AuthDbContext _db;
    private readonly IConfiguration _config;
    public AuthController(UserManager<IdentityUser> users, AuthDbContext db, IConfiguration config){_users=users;_db=db;_config=config;}

    [HttpGet("groups/check")]
    public async Task<IActionResult> CheckGroup([FromQuery]string name)
    {
        var normalized=Normalize(name); if(string.IsNullOrWhiteSpace(normalized)) return BadRequest("Informe o nome do grupo.");
        var group=await _db.Groups.AsNoTracking().FirstOrDefaultAsync(x=>x.NormalizedName==normalized);
        return Ok(new { exists=group is not null, name=group?.Name ?? name.Trim() });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if(string.IsNullOrWhiteSpace(request.GroupName)) return BadRequest("Informe o grupo.");
        if(await _users.FindByEmailAsync(request.Email) is not null) return Conflict("E-mail já cadastrado.");
        var user=new IdentityUser{UserName=request.Username.Trim(),Email=request.Email.Trim()};
        var result=await _users.CreateAsync(user,request.Password); if(!result.Succeeded) return BadRequest(result.Errors);
        try { var membership=await JoinOrCreateGroup(user.Id,request.GroupName); return Ok(AuthState(user,membership,null)); }
        catch { await _users.DeleteAsync(user); throw; }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user=await _users.FindByNameAsync(request.Username) ?? await _users.FindByEmailAsync(request.Username);
        if(user is null || !await _users.CheckPasswordAsync(user,request.Password)) return Unauthorized();
        return Ok(await BuildLogin(user));
    }

    [HttpPost("google")]
    public async Task<IActionResult> Google(GoogleLoginRequest request)
    {
        var clientId=_config["Google:ClientId"]; if(string.IsNullOrWhiteSpace(clientId)) return StatusCode(503,"Login Google não configurado.");
        GoogleJsonWebSignature.Payload payload;
        try { payload=await GoogleJsonWebSignature.ValidateAsync(request.IdToken,new GoogleJsonWebSignature.ValidationSettings{Audience=[clientId]}); }
        catch { return Unauthorized("Token Google inválido."); }
        var user=await _users.FindByEmailAsync(payload.Email);
        if(user is null){ user=new IdentityUser{UserName=payload.Email,Email=payload.Email,EmailConfirmed=true}; var created=await _users.CreateAsync(user); if(!created.Succeeded)return BadRequest(created.Errors); return Ok(new{requiresGroup=true,userId=user.Id,email=user.Email,name=payload.Name}); }
        return Ok(await BuildLogin(user));
    }

    [Authorize]
    [HttpPost("group")]
    public async Task<IActionResult> ChooseGroup(GroupChoiceRequest request)
    {
        var user=await CurrentUser(); if(user is null)return Unauthorized();
        if(await _db.GroupMemberships.AnyAsync(x=>x.UserId==user.Id&&x.Status!=GroupMemberStatus.Rejected))return Conflict("Usuário já possui grupo ou solicitação pendente.");
        var membership=await JoinOrCreateGroup(user.Id,request.GroupName); return Ok(AuthState(user,membership,null));
    }

    [Authorize]
    [HttpGet("group/members")]
    public async Task<IActionResult> Members()
    {
        var membership=await CurrentMembership(); if(membership is null)return Forbid();
        var members=await _db.GroupMemberships.Where(x=>x.GroupId==membership.GroupId).OrderBy(x=>x.Status).ToListAsync();
        var result=new List<object>(); foreach(var m in members){var u=await _users.FindByIdAsync(m.UserId);result.Add(new{id=m.Id,email=u?.Email,username=u?.UserName,status=m.Status.ToString(),role=m.Role.ToString()});} return Ok(result);
    }

    [Authorize]
    [HttpPut("group/members/{id:guid}")]
    public async Task<IActionResult> Decide(Guid id,MembershipDecisionRequest request)
    {
        var owner=await CurrentMembership(); if(owner is null||owner.Role!=GroupMemberRole.Owner||owner.Status!=GroupMemberStatus.Active)return Forbid();
        var target=await _db.GroupMemberships.FirstOrDefaultAsync(x=>x.Id==id&&x.GroupId==owner.GroupId); if(target is null)return NotFound(); if(target.Role==GroupMemberRole.Owner)return BadRequest("O gestor não pode ser alterado.");
        target.Status=request.Approve?GroupMemberStatus.Active:GroupMemberStatus.Rejected; await _db.SaveChangesAsync(); return NoContent();
    }

    private async Task<object> BuildLogin(IdentityUser user){var membership=await _db.GroupMemberships.Include(x=>x.Group).FirstOrDefaultAsync(x=>x.UserId==user.Id&&x.Status!=GroupMemberStatus.Rejected);return AuthState(user,membership,membership?.Status==GroupMemberStatus.Active?CreateToken(user,membership):null);}
    private object AuthState(IdentityUser user,GroupMembership? membership,string? token)=>new{token,username=user.UserName,email=user.Email,requiresGroup=membership is null,pendingApproval=membership?.Status==GroupMemberStatus.Pending,group=membership is null?null:new{id=membership.GroupId,name=membership.Group.Name,role=membership.Role.ToString()},message=membership?.Status==GroupMemberStatus.Pending?"Solicitação enviada ao gestor do grupo.":null};
    private async Task<GroupMembership> JoinOrCreateGroup(string userId,string name){var normalized=Normalize(name);var group=await _db.Groups.Include(x=>x.Memberships).FirstOrDefaultAsync(x=>x.NormalizedName==normalized);if(group is null){group=new CashFlowGroup{Name=name.Trim(),NormalizedName=normalized,OwnerUserId=userId};var owner=new GroupMembership{Group=group,UserId=userId,Role=GroupMemberRole.Owner,Status=GroupMemberStatus.Active};_db.AddRange(group,owner);await _db.SaveChangesAsync();return owner;}var pending=new GroupMembership{GroupId=group.Id,Group=group,UserId=userId,Role=GroupMemberRole.Member,Status=GroupMemberStatus.Pending};_db.Add(pending);await _db.SaveChangesAsync();return pending;}
    private string CreateToken(IdentityUser user,GroupMembership membership){var claims=new[]{new Claim(ClaimTypes.NameIdentifier,user.Id),new Claim(ClaimTypes.Name,user.UserName!),new Claim(ClaimTypes.Email,user.Email??""),new Claim("group_id",membership.GroupId.ToString()),new Claim("group_role",membership.Role.ToString())};var key=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));var token=new JwtSecurityToken(_config["Jwt:Issuer"],_config["Jwt:Audience"],claims,expires:DateTime.UtcNow.AddHours(8),signingCredentials:new SigningCredentials(key,SecurityAlgorithms.HmacSha256));return new JwtSecurityTokenHandler().WriteToken(token);}
    private async Task<IdentityUser?> CurrentUser()=>await _users.GetUserAsync(User);
    private async Task<GroupMembership?> CurrentMembership(){var uid=User.FindFirstValue(ClaimTypes.NameIdentifier);return uid is null?null:await _db.GroupMemberships.FirstOrDefaultAsync(x=>x.UserId==uid);}
    private static string Normalize(string name)=>name.Trim().ToUpperInvariant();
}