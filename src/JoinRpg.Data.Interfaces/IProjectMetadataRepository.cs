namespace JoinRpg.Data.Interfaces;

public interface IProjectMetadataRepository
{
    /// <exception cref="ProjectNotFoundException">Проект не найден.</exception>
    Task<ProjectInfo> GetProjectMetadata(ProjectIdentification projectId, bool ignoreCache = false);

    /// <summary>
    /// Догружает к уже собранным метаданным то, что в них не кладётся из-за размера: анонс, правила
    /// подачи, условия льготного взноса, карточки КогдаИгры.
    /// </summary>
    /// <remarks>
    /// Принимает готовый <see cref="ProjectInfo"/>, чтобы не собирать его заново: граф проекта для
    /// метаданных — это десяток Include, а EF6 выполняет запрос повторно, даже если проект уже
    /// загружен в контекст. Вызывающим удобнее перегрузка по id из
    /// <see cref="ProjectMetadataRepositoryExtensions"/> — она берёт метаданные из кеша на запрос.
    /// </remarks>
    /// <exception cref="ProjectNotFoundException">Проект не найден.</exception>
    Task<ProjectDetails> GetProjectDetails(ProjectInfo projectInfo);

    /// <summary>
    /// Кладёт свежесобранный <paramref name="projectInfo"/> в кэш (если он есть), чтобы
    /// последующие чтения в рамках того же запроса видели актуальное состояние после изменения.
    /// Без кэша — no-op.
    /// </summary>
    void PrimeCache(ProjectInfo projectInfo);
}

public static class ProjectMetadataRepositoryExtensions
{
    /// <summary>
    /// Детали проекта по id: метаданные — через <see cref="IProjectMetadataRepository.GetProjectMetadata"/>
    /// (из кеша на запрос, если он есть), остальное — узким запросом.
    /// </summary>
    /// <exception cref="ProjectNotFoundException">Проект не найден.</exception>
    public static async Task<ProjectDetails> GetProjectDetails(this IProjectMetadataRepository repository, ProjectIdentification projectId)
        => await repository.GetProjectDetails(await repository.GetProjectMetadata(projectId));
}
