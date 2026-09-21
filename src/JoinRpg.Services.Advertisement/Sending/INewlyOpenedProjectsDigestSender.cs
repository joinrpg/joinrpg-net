namespace JoinRpg.Services.Advertisement.Sending;

internal interface INewlyOpenedProjectsDigestSender
{
    Task<SendingResult> Send(IReadOnlyList<(ProjectAdvertisementCandidate Project, KogdaIgraGameData Game)> entries);
}
