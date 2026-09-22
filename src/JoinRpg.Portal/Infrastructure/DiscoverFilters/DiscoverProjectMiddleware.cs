using JoinRpg.Data.Interfaces;
using Serilog.Context;

namespace JoinRpg.Portal.Infrastructure.DiscoverFilters;

/// <summary>
/// Store projectId for CurrentProjectAccessor
/// </summary>
/// <remarks>
/// Это чисто синтаксический разбор запроса: из пути (или из query) достаётся число, в базу
/// middleware не ходит и существование проекта не проверяет. Контракт HttpContext.Items здесь —
/// «какой проект запрошен», а не «какой проект существует»: на этом значении строятся
/// Serilog-обогащение логов, привязка моделей и проверки доступа, и им важно видеть исходный
/// запрошенный id даже тогда, когда проекта нет.
/// Проверять существование прямо тут означало бы тянуть тяжёлую загрузку метаданных проекта
/// (ProjectLoaderCommon.GetProjectWithFieldsAsync) на каждый запрос с числом в первом сегменте —
/// до роутинга и аутентификации, в том числе для ботового мусора, который в кеш не попадает.
/// Поэтому «проекта нет» обрабатывают потребители: <see cref="IProjectMetadataRepository"/>
/// бросает <see cref="JoinRpgEntityNotFoundException"/>, и это штатный исход, а не ошибка.
/// </remarks>
public class DiscoverProjectMiddleware(RequestDelegate nextDelegate)
{

    /// <inheritedoc />
    public async Task InvokeAsync(HttpContext context)
    {
        HttpRequest request = context.Request;

        if ((request.Path.TryExtractFromPath() ?? request.Query.TryExtractFromQuery()) is ProjectIdentification projectId)
        {
            context.Items[Constants.ProjectIdName] = projectId.Value;
            _ = LogContext.PushProperty(Constants.ProjectIdName, projectId);
        }

        await nextDelegate(context);
    }


}
