namespace JoinRpg.Services.Advertisement.Sending;

internal interface ISingleHotRoleSender
{
    Task<SendingResult> Send(ProjectInfo project, DomainTypes.ProjectMetadata.ProjectDetails details, Character character, Uri addClaimUri);
}
