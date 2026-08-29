namespace CashFlow.Auth.Api.Models;

public sealed class CashFlowGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string OwnerUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<GroupMembership> Memberships { get; set; } = new List<GroupMembership>();
}

public sealed class GroupMembership
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public GroupMemberStatus Status { get; set; }
    public GroupMemberRole Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public CashFlowGroup Group { get; set; } = null!;
}

public enum GroupMemberStatus { Pending = 0, Active = 1, Rejected = 2 }
public enum GroupMemberRole { Member = 0, Owner = 1 }

public sealed record GroupChoiceRequest(string GroupName);
public sealed record GoogleLoginRequest(string IdToken);
public sealed record MembershipDecisionRequest(bool Approve);
