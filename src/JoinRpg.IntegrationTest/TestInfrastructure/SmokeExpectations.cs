using System.Net;

namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// Чего смоук ждёт от каждого GET-эндпоинта: 200, другой код или «не ходим, потому что…» (#4956).
/// </summary>
/// <remarks>
/// Списки ведутся вручную по одной причине: эндпоинт не должен пропадать из-под проверки молча.
/// Новая страница, до которой смоук не смог дойти, валит тест с именем маршрута — и её нужно
/// либо досеять в <see cref="SmokeProjectFixture"/>, либо записать сюда с причиной.
/// Обратная проверка тоже есть: запись про несуществующий маршрут валит тест как устаревшая.
/// </remarks>
internal static class SmokeExpectations
{
    /// <summary>Эндпоинты, по которым смоук не ходит, и почему.</summary>
    public static readonly IReadOnlyDictionary<string, string> Skipped = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        // Мутирующие GET-запросы. Смоук обходит все страницы на одном сиде, поэтому такой запрос
        // испортил бы данные следующим кейсам. Легаси: по-хорошему это POST (#4956).
        ["{projectId}/rooms/DeleteRoomType"] = "GET удаляет тип поселения — снёс бы сид",
        ["{projectId}/rooms/OccupyAll"] = "GET расселяет всех по комнатам — изменил бы сид",
        // UnOccupyAll переведён на POST (ADR018, §14), выселение по типу — POST-ручка острова
        // комнат: смоук их больше не видит.

        // Права админа, а смоук ходит под мастером проекта. Отдельный кейс, в этой итерации не тащим.
        ["{projectId}/masters/force-admin-access"] = "нужны права админа сайта, а не мастера проекта",

        // Нужны данные, которых сид не создаёт.
        ["{projectId}/plots/ByTag"] = "нужен сюжетный тег, сид его не создаёт",
        ["{projectId}/reports/2d/{gameReport2DTemplateId}"] = "нужен сохранённый шаблон 2D-отчёта, сид его не создаёт",
        ["{projectId}/money/SummaryByMaster"] = "нужен токен выгрузки, выдаётся отдельной страницей",
        // JWT тут не нужен — расписание отдаётся анонимно, доступ решает видимость полей
        // времени и локации. Смоук всё равно не ходит: в его сиде расписание не настроено.
        ["x-game-api/{projectId}/schedule/all"] = "нужно настроенное расписание, покрыт XApiScheduleTests",
        ["{projectId}/rooms/report"] = "не HTML-страница: без параметра export отдаёт 404, покрыто AccommodationPagesLazyLoadsScenario",
        ["{projectId}/plot/{**path}"] = "catch-all легаси-редиректа: осмысленного значения для path нет",

        // JWT-авторизация: cookie мастера тут не работает. Эти маршруты покрыты тестами XApi,
        // и их долг уже виден в снапшоте ленивых загрузок.
        ["x-api/me/projects/active"] = "нужен JWT, покрыто тестами XApi",
        ["x-api/users/{userId:int}"] = "нужен JWT, покрыто тестами XApi",
        ["x-game-api/schedule/projects/active"] = "нужен JWT, покрыто тестами XApi",
        ["x-game-api/{projectId}/characters"] = "нужен JWT, покрыто тестами XApi",
        ["x-game-api/{projectId}/characters/by-ids"] = "нужен JWT, покрыто тестами XApi",
        ["x-game-api/{projectId}/characters/{characterId}"] = "нужен JWT, покрыто тестами XApi",
        ["x-game-api/{projectId}/checkin/allclaims"] = "нужен JWT, покрыто тестами XApi",
        ["x-game-api/{projectId}/checkin/stat"] = "нужен JWT, покрыто тестами XApi",
        ["x-game-api/{projectId}/checkin/{claimId}/prepare"] = "нужен JWT, покрыто тестами XApi",
        ["x-game-api/{projectId}/claims/{claimId}"] = "нужен JWT, покрыто тестами XApi",
        ["x-game-api/{projectId}/groups/{groupId}/characters"] = "нужен JWT, покрыто тестами XApi",
        ["x-game-api/{projectId}/metadata/fields"] = "нужен JWT, покрыто тестами XApi",

        // Страницы админки сайта, не проекта.
        ["Admin"] = "админка сайта, смоук ходит под мастером проекта",
        ["Admin/Index"] = "админка сайта, смоук ходит под мастером проекта",
        ["Admin/Jobs"] = "админка сайта, смоук ходит под мастером проекта",
        ["admin/advertisement"] = "админка сайта, смоук ходит под мастером проекта",
        ["admin/cmp-tests"] = "админка сайта, смоук ходит под мастером проекта",
        ["admin/hot-roles"] = "админка сайта, смоук ходит под мастером проекта",
        ["admin/kogda-igra-sync"] = "админка сайта, смоук ходит под мастером проекта",
        ["admin/notifications"] = "админка сайта, смоук ходит под мастером проекта",
        ["admin/test-message"] = "админка сайта, смоук ходит под мастером проекта",
        ["webapi/UserAdmin/GetAdminPanel"] = "админка сайта, смоук ходит под мастером проекта",
        ["webapi/projects/GetProjectsForAdmin"] = "админка сайта, смоук ходит под мастером проекта",
        ["webapi/kogdaigra/GetFutureKogdaIgraCandidates"] = "админка сайта, смоук ходит под мастером проекта",
        ["webapi/kogdaigra/GetKogdaIgraCandidates"] = "админка сайта, смоук ходит под мастером проекта",
        ["webapi/kogdaigra/GetKogdaIgraNotUpdated"] = "админка сайта, смоук ходит под мастером проекта",
        ["webapi/kogdaigra/GetSyncStatus"] = "админка сайта, смоук ходит под мастером проекта",

        // Служебные страницы без данных проекта.
        ["About/Donate"] = "нужен существующий проект-копилка (его id зашит в конфигурации), в тестовой базе его нет",
        ["error/{HttpStatusCode:int?}"] = "страница ошибки, к проекту не относится",
        ["Error/Antiforgery"] = "страница ошибки, к проекту не относится",
    };

    /// <summary>
    /// Эндпоинты, которые отвечают не 200, и какой код от них ждать.
    /// </summary>
    /// <remarks>
    /// Клиент смоука не ходит по редиректам специально: иначе 302 на страницу входа выглядел бы
    /// как честный 200. Поэтому редирект-эндпоинты (легаси-ссылки, переходы к комментарию)
    /// перечислены тут явно — их 302 и есть правильный ответ.
    /// </remarks>
    public static readonly IReadOnlyDictionary<string, HttpStatusCode> ExpectedStatus =
        new Dictionary<string, HttpStatusCode>(StringComparer.Ordinal)
        {
            // Легаси-адреса: переехали навсегда.
            ["claimlist/my"] = HttpStatusCode.MovedPermanently,
            ["my/claims"] = HttpStatusCode.MovedPermanently,
            ["{ProjectId}/roles/{CharacterGroupId}/discussing"] = HttpStatusCode.MovedPermanently,

            // Короткие ссылки: ведут на страницу, а сами её не показывают.
            [".well-known/change-password"] = HttpStatusCode.Redirect,
            ["user/me"] = HttpStatusCode.Redirect,
            ["{ProjectId}/goto/comment/{CommentId:int}"] = HttpStatusCode.Redirect,
            ["{ProjectId}/goto/discussion/{CommentDiscussionId}"] = HttpStatusCode.Redirect,
            ["{ProjectId}/goto/finance-operation/{FinanceOperationId:int}"] = HttpStatusCode.Redirect,
            ["{projectId}/subscribe/EditRedirect"] = HttpStatusCode.Redirect,
            ["{projectId}/subscribe/EditForGroup/{characterGroupId}"] = HttpStatusCode.Redirect,
            ["{projectid}/apply"] = HttpStatusCode.Redirect,
            ["{projectid}/roles/{characterGroupId}/apply"] = HttpStatusCode.Redirect,

            // Заявки самого мастера в сид-проекте нет, поэтому «моя заявка» уводит на список.
            ["{ProjectId}/claim/{ClaimId}/MyClaim"] = HttpStatusCode.Redirect,
            ["{projectId}/myclaim"] = HttpStatusCode.Redirect,

            // userId в смоуке — владелец, он уже мастер: добавление ведёт на страницу правки (ADR019, §1).
            ["{projectId}/masters/add/{userId}"] = HttpStatusCode.Redirect,

            // Вторая роль предлагается только по заявке, прошедшей чек-ин.
            ["{ProjectId}/claim/{ClaimId}/secondrole"] = HttpStatusCode.Redirect,

            // В сиде расписание не настроено, а подписке на календарь без расписания честнее 404 (#5266).
            ["{projectId}/schedule/ical"] = HttpStatusCode.NotFound,
        };

    public static HttpStatusCode ExpectedStatusFor(SmokeEndpoint endpoint)
        => ExpectedStatus.GetValueOrDefault(endpoint.RouteTemplate, HttpStatusCode.OK);
}
