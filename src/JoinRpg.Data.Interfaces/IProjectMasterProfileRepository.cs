namespace JoinRpg.Data.Interfaces;

/// <summary>
/// Роль и описание мастера — для страницы мастеров. В слепок ProjectInfo не входят (ADR019, §4):
/// нужны только этой странице, а описание бывает длинным.
/// </summary>
public record ProjectMasterProfileDto(UserIdentification UserId, MasterRoleTitle Role, MarkdownString? Description);

public interface IProjectMasterProfileRepository
{
    /// <summary>Профили действующих мастеров проекта.</summary>
    Task<IReadOnlyCollection<ProjectMasterProfileDto>> GetMasterProfiles(ProjectIdentification projectId);

    /// <summary>
    /// Профиль мастера в любом статусе — чтобы при возвращении бывшего мастера предзаполнить форму прежним профилем.
    /// </summary>
    Task<ProjectMasterProfileDto?> GetMasterProfile(ProjectIdentification projectId, UserIdentification userId);
}
