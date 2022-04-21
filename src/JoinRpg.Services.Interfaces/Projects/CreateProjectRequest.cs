using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Services.Interfaces.Projects;

public record CreateProjectRequest(ProjectName ProjectName, ProjectTypeDto ProjectType);

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
