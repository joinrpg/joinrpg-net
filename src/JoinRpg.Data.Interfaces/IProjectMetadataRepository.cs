namespace JoinRpg.Data.Interfaces;

public interface IProjectMetadataRepository
{
    Task<ProjectInfo> GetProjectMetadata(ProjectIdentification projectId, bool ignoreCache = false);
    Task<ProjectDetails> GetProjectDetails(ProjectIdentification projectId);

    /// <summary>
    /// Кладёт свежесобранный <paramref name="projectInfo"/> в кэш (если он есть), чтобы
    /// последующие чтения в рамках того же запроса видели актуальное состояние после изменения.
    /// Без кэша — no-op.
    /// </summary>
    void PrimeCache(ProjectInfo projectInfo);
}
