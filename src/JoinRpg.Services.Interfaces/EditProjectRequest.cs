namespace JoinRpg.Services.Interfaces;

public class EditProjectRequest
{
    public required ProjectIdentification ProjectId { get; set; }
    public required string ProjectName { get; set; }
    public required string ClaimApplyRules { get; set; }
    public required string ProjectAnnounce { get; set; }
}
