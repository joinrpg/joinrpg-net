namespace JoinRpg.Services.Interfaces.ProjectAccess;

public class AccessRequestBase
{
    public required ProjectIdentification ProjectId { get; set; }
    public required UserIdentification UserId { get; set; }
    public Permission[] Permissions { get; set; } = [];
}
