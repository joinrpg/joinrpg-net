using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Services.Interfaces.Projects;

public record CreateProjectRequest
{
    internal CreateProjectRequest(ProjectName ProjectName, ProjectTypeDto ProjectType)
    {
        this.ProjectName = ProjectName;
        this.ProjectType = ProjectType;
    }

    public ProjectName ProjectName { get; }
    public ProjectTypeDto ProjectType { get; }

    public static CreateProjectRequest Create(ProjectName ProjectName, ProjectTypeDto ProjectType, ProjectIdentification? CopyFromId, ProjectCopySettingsDto CopySettings)
    {
        if (CopyFromId is not null && ProjectType == ProjectTypeDto.CopyFromAnother)
        {
            return new CloneProjectRequest(ProjectName, CopyFromId, CopySettings);
        }
        if (ProjectType != ProjectTypeDto.CopyFromAnother)
        {
            return new CreateProjectRequest(ProjectName, ProjectType);
        }
        throw new ArgumentException("Incorrect combination of parameters");
    }
}

public abstract record CreateProjectResultBase
{

}

public record PartiallySuccessCreateProjectResult(ProjectIdentification ProjectId, string Message) : CreateProjectResultBase()
{

}

public record SuccessCreateProjectResult(ProjectIdentification ProjectId) : CreateProjectResultBase()
{

}

public record FaildToCreateProjectResult(string Message) : CreateProjectResultBase()
{

}

public record CloneProjectRequest(ProjectName ProjectName, ProjectIdentification CopyFromId, ProjectCopySettingsDto CopySettings) : CreateProjectRequest(ProjectName, ProjectTypeDto.CopyFromAnother)
{

}
