using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Services.Interfaces.Projects;

public record CreateProjectRequest
{
    public CreateProjectRequest(ProjectName ProjectName, ProjectTypeDto ProjectType, ProjectIdentification? CopyFromId)
    {
        if (ProjectType == ProjectTypeDto.CopyFrom)
        {
            ArgumentNullException.ThrowIfNull(CopyFromId);
        }
        else
        {
            if (CopyFromId is not null)
            {
                throw new ArgumentException("Should be null if ProjectTypeDto!=CopyFrom", nameof(CopyFromId);
            }
        }
        this.ProjectName = ProjectName;
        this.ProjectType = ProjectType;
        this.CopyFromId = CopyFromId;
    }

    public ProjectName ProjectName { get; }
    public ProjectTypeDto ProjectType { get; }
    public ProjectIdentification? CopyFromId { get; }
}

public record ProjectName : SingleValueType<string>
{
    public ProjectName(string value) : base(value.Trim())
    {
        if (Value.Length > 100)
        {
            throw new ArgumentException("Project name is too long", nameof(value));
        }
        if (Value.Length < 5)
        {
            throw new ArgumentException("Project name is too long", nameof(value));
        }
    }
}
