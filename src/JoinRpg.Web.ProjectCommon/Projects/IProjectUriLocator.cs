namespace JoinRpg.Web.ProjectCommon.Projects;

public interface IProjectUriLocator
{
    Uri GetMyClaimUri(ProjectIdentification projectId);
    Uri GetAddClaimUri(ProjectIdentification projectId);
    Uri GetCreatePlotUri(ProjectIdentification projectId);

    Uri GetRolesListUri(ProjectIdentification projectId);
    Uri GetCaptainCabinetUri(ProjectIdentification projectId);

    /// <summary>Создание персонажа без привязки к группе. Для группы — <see cref="ICharacterGroupUriLocator.GetCreateCharacterUri"/>.</summary>
    Uri GetCreateCharacterUri(ProjectIdentification projectId);

    /// <summary>Рассылка по выбранным заявкам.</summary>
    Uri GetMassMailUri(ProjectIdentification projectId, IReadOnlyCollection<ClaimIdentification> claimIds);

    /// <summary>Печать выбранных персонажей.</summary>
    Uri GetPrintCharactersUri(ProjectIdentification projectId, IReadOnlyCollection<CharacterIdentification> characterIds);
}
