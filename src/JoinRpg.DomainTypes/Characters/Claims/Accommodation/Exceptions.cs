using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Characters.Claims.Accommodation;

/// <summary>
/// Приглашение к совместному проживанию отправить нельзя. Причина отказа приходит в
/// <paramref name="message"/> и показывается игроку.
/// </summary>
public class AccommodationInviteNotAllowedException(ProjectIdentification projectId, string message)
    : JoinRpgProjectException(projectId, message);
