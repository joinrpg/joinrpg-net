namespace JoinRpg.Services.Interfaces.Projects;

public record CreateProjectRequest
{
    internal CreateProjectRequest(ProjectName ProjectName, ProjectTypeDto ProjectType,
        KogdaIgraLinkChoiceDto KogdaIgraChoice, KogdaIgraIdentification? KogdaIgraGameId, string? MessageForKogdaIgraEditors)
    {
        this.ProjectName = ProjectName;
        this.ProjectType = ProjectType;
        this.KogdaIgraChoice = KogdaIgraChoice;
        this.KogdaIgraGameId = KogdaIgraGameId;
        this.MessageForKogdaIgraEditors = MessageForKogdaIgraEditors;
    }

    public ProjectName ProjectName { get; }
    public ProjectTypeDto ProjectType { get; }
    public KogdaIgraLinkChoiceDto KogdaIgraChoice { get; }
    public KogdaIgraIdentification? KogdaIgraGameId { get; }
    public string? MessageForKogdaIgraEditors { get; }

    public static CreateProjectRequest Create(ProjectName ProjectName, ProjectTypeDto ProjectType, ProjectIdentification? CopyFromId, ProjectCopySettingsDto CopySettings,
        KogdaIgraLinkChoiceDto KogdaIgraChoice, KogdaIgraIdentification? KogdaIgraGameId, string? MessageForKogdaIgraEditors)
    {
        if (CopyFromId is not null && ProjectType == ProjectTypeDto.CopyFromAnother)
        {
            return new CloneProjectRequest(ProjectName, CopyFromId, CopySettings, KogdaIgraChoice, KogdaIgraGameId, MessageForKogdaIgraEditors);
        }
        if (ProjectType != ProjectTypeDto.CopyFromAnother)
        {
            return new CreateProjectRequest(ProjectName, ProjectType, KogdaIgraChoice, KogdaIgraGameId, MessageForKogdaIgraEditors);
        }
        throw new ArgumentException("Incorrect combination of parameters");
    }
}

public enum KogdaIgraLinkChoiceDto
{
    Linked,
    NotOnKogdaIgra,
    ShouldNotBeOnKogdaIgra,
    Trial,
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

public record CloneProjectRequest(ProjectName ProjectName, ProjectIdentification CopyFromId, ProjectCopySettingsDto CopySettings,
    KogdaIgraLinkChoiceDto KogdaIgraChoice, KogdaIgraIdentification? KogdaIgraGameId, string? MessageForKogdaIgraEditors)
    : CreateProjectRequest(ProjectName, ProjectTypeDto.CopyFromAnother, KogdaIgraChoice, KogdaIgraGameId, MessageForKogdaIgraEditors)
{

}
