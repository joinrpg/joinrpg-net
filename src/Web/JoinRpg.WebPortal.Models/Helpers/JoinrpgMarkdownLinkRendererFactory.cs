using JoinRpg.Data.Interfaces;
using JoinRpg.Markdown;

namespace JoinRpg.Web.Models.Helpers;

/// <summary>
/// Единственное место, которое знает, какие данные нужны <see cref="JoinrpgMarkdownLinkRenderer"/>,
/// и как их загрузить. До неё каждый вызывающий грузил EF-граф проекта сам и передавал во вьюмодель
/// сущность <c>Project</c> — способы разъезжались, а вьюмодели сюжетов из-за этого зависели от EF.
/// </summary>
/// <remarks>
/// <see cref="Load"/> — дорогой запрос (все персонажи проекта с заявками и игроками утверждённых
/// заявок), поэтому звать его нужно только там, где текст вводных действительно рендерится.
/// Следующий шаг — сканировать текст на директивы и грузить граф только при попадании; тогда эта
/// цена останется у считанных проектов, которые директивами реально пользуются.
///
/// Фабрика — точка, через которую пойдёт отвязка рендерера от EF-сущности <c>Project</c> (#4923):
/// менять источник данных придётся здесь, а не в каждом контроллере.
/// </remarks>
public class JoinrpgMarkdownLinkRendererFactory(
    IProjectRepository projectRepository,
    IProjectMetadataRepository projectMetadataRepository)
{
    /// <summary>
    /// Рендерер, который не разворачивает ничего. Для страниц, где рендерить нечего — чтобы не
    /// платить за загрузку графа и не тащить вниз nullable-рендерер.
    /// </summary>
    public static ILinkRenderer NoDirectives => DoNothingLinkRenderer.Instance;

    /// <summary>
    /// Кэш на время запроса: в отличие от <see cref="IProjectMetadataRepository"/>, репозитории
    /// проекта ничего не кэшируют, и два рендеринга на одной странице означали бы два тяжёлых
    /// запроса.
    /// </summary>
    private readonly Dictionary<ProjectIdentification, ILinkRenderer> loaded = [];

    /// <summary>
    /// Грузит EF-граф проекта и собирает рендерер. Дорого — см. remarks класса.
    /// </summary>
    public async Task<ILinkRenderer> Load(ProjectIdentification projectId)
    {
        if (loaded.TryGetValue(projectId, out var cached))
        {
            return cached;
        }

        var project = await projectRepository.GetProjectForMarkdownRendering(projectId);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var renderer = new JoinrpgMarkdownLinkRenderer(project, projectInfo);
        loaded[projectId] = renderer;
        return renderer;
    }
}
