namespace JoinRpg.Services.Interfaces;

public class EditProjectRequest
{
    public required ProjectIdentification ProjectId { get; set; }
    public required ProjectName ProjectName { get; set; }
    public required string ProjectAnnounce { get; set; }
}
