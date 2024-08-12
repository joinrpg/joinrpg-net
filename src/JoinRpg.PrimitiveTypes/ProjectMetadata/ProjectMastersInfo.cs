namespace JoinRpg.PrimitiveTypes.ProjectMetadata;
public record class ProjectMastersListInfo
    (ProjectIdentification ProjectId,
    string ProjectName,
    IReadOnlyCollection<ProjectMasterInfo> Masters)
{
}

public record class ProjectMasterInfo(UserIdentification UserId, UserDisplayName Name, Email Email)
{
}
