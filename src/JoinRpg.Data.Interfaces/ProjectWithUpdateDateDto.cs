namespace JoinRpg.Data.Interfaces;

public class ProjectWithUpdateDateDto
{
    public int ProjectId { get; set; }
    public required string ProjectName { get; set; }
    public DateTime LastUpdated { get; set; }
}
