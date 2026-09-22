namespace JoinRpg.Data.Interfaces;

public interface IProjectMetadataRepository
{
    /// <exception cref="JoinRpgEntityNotFoundException">Проект не найден.</exception>
    Task<ProjectInfo> GetProjectMetadata(ProjectIdentification projectId, bool ignoreCache = false);

    /// <exception cref="JoinRpgEntityNotFoundException">Проект не найден.</exception>
    Task<ProjectDetails> GetProjectDetails(ProjectIdentification projectId);

    /// <summary>
    /// Кладёт свежесобранный <paramref name="projectInfo"/> в кэш (если он есть), чтобы
    /// последующие чтения в рамках того же запроса видели актуальное состояние после изменения.
    /// Без кэша — no-op.
    /// </summary>
    void PrimeCache(ProjectInfo projectInfo);
}
