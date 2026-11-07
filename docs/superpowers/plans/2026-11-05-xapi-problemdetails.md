# ProblemDetails для XApi — план реализации

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Все ошибки внешнего XApi (`/x-api`, `/x-game-api`) отдаются как JSON ProblemDetails, а не пустое тело или HTML-страница `/error`.

**Architecture:** Расширяем существующий механизм ProblemDetails для `/webapi/`: `WebApiExceptionHandler` начинает покрывать внешние API-пути, новый `IAuthorizationMiddlewareResultHandler` пишет ProblemDetails для 401/403, а `UseStatusCodePages` ветвится: внешнему API — ProblemDetails, остальным — прежний re-execute HTML-страницы.

**Tech Stack:** ASP.NET Core 10, `IProblemDetailsService`, JWT Bearer, xUnit + Shouldly.

**Spec:** `docs/superpowers/specs/2026-11-05-xapi-problemdetails-design.md`

## Global Constraints

- Работать только в текущем worktree `joinrpg-net.xapi-unauthorized`; соседний `../joinrpg-net` не трогать.
- Без регулярных выражений.
- Файлы `.cs` — CRLF: править только через редактор (Read/Write/Edit), не через `sed`/`awk`/shell-редирект.
- Видимые пользователю строки — на русском (заголовки ProblemDetails уже русские в `WebApiClientErrorTitles`).
- Статус-коды ответов не меняются — только тело.
- Коммиты: `git commit -m "..."` (без `--edit`); перед коммитом `dotnet format`.
- Интеграционные тесты локально — только через `--filter` на конкретный тестовый класс (`JoinRpg.IntegrationTest` поднимает контейнер с БД).

## Review Focus

- Запрос вообще без токена → 401 ProblemDetails, заголовок `WWW-Authenticate` сохраняется (Task 2 юнит + Task 4 интеграция).
- Токен валидный, но пользователь — не мастер проекта → 403 ProblemDetails (Task 4).
- Несуществующий `projectId` → доменный `ProjectNotFoundException` → 404 ProblemDetails, а не 500 с HTML (Task 1 юнит + Task 4).
- Маршрут не совпал (несуществующий endpoint под `/x-game-api`) → 404 ProblemDetails через ветвь StatusCodePages, а не HTML `/error/404` (Task 4).
- Поведение страниц и `/webapi/` не меняется: cookie-редиректы, HTML-страницы ошибок остаются (Task 3 ветвит только внешние API; Task 2 делегирует дефолту) — покрыто юнит-тестами «не-API путь делегируется дефолту».

---

### Task 1: `IsApiPath` и ProblemDetails по исключениям для XApi

**Files:**
- Modify: `src/JoinRpg.Portal/Infrastructure/ApiPathHelper.cs`
- Modify: `src/JoinRpg.Portal/Infrastructure/WebApiExceptionHandler.cs:18`
- Test: `src/JoinRpg.Portal.Test/Infrastructure/WebApiExceptionHandlerTest.cs`

**Interfaces:**
- Consumes: ничего нового.
- Produces: `ApiPathHelper.IsApiPath(this PathString path)` — true для `/webapi`, `/x-api`, `/x-game-api`; `ApiPathHelper.IsExternalApiPath(this PathString path)` — true только для `/x-api`, `/x-game-api` (у существующего `IsExternalApiPath(this string)` оверлoad на `PathString`). `WebApiExceptionHandler` обрабатывает исключения на всех `IsApiPath`-путях.

- [ ] **Step 1: Написать падающие юнит-тесты в `WebApiExceptionHandlerTest.cs`**

По образцу существующих тестов класса (тот же `CreateHttpContext`/`Handle`):

```csharp
[Fact]
public async Task NoAccessToProject_InXGameApi_ReturnsForbiddenWithReason()
{
    var httpContext = CreateHttpContext("/x-game-api/1611/characters");
    var (handled, problem) = await Handle(httpContext, new NoAccessToProjectException(ProjectId, userId: null));
    handled.ShouldBeTrue();
    httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    problem.GetProperty("title").GetString().ShouldBe("Недостаточно прав для этого действия");
}

[Fact]
public async Task ProjectNotFound_InXApi_ReturnsNotFoundWithReason()
{
    var httpContext = CreateHttpContext("/x-api/users/1/");
    var (handled, problem) = await Handle(httpContext, new ProjectNotFoundException(ProjectId));
    handled.ShouldBeTrue();
    httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    problem.GetProperty("title").GetString().ShouldBe("Проект не найден");
}
```

Плюс параметризованный/два теста «вне API по-прежнему не обрабатывается»: путь `/x-api-fake/...` (префикс `/x-api`, но не сегмент) и обычная страница `/1611/project/settings` → `handled.ShouldBeFalse()`. Конструктор `NoAccessToProjectException` — посмотреть сигнатуру через `find_symbol`, подставить реальные аргументы.

- [ ] **Step 2: Запустить, убедиться что падает**

Run: `dotnet test src/JoinRpg.Portal.Test --filter "FullyQualifiedName~WebApiExceptionHandlerTest"`
Expected: FAIL — `/x-api/...` пути возвращают `handled == false`.

- [ ] **Step 3: Реализовать**

В `ApiPathHelper.cs`:
- `public static bool IsApiPath(this PathString path) => path.IsInternalApiPath() || path.IsExternalApiPath();`
- `public static bool IsExternalApiPath(this PathString path)` — сегментная проверка первого сегмента (`x-api`/`x-game-api` через существующий `IsApiPathSegment` на `path.StartsWithSegments("/")`… проще: `path.HasValue && IsApiPathSegment(path.Value.TrimStart('/').Split('/')[0].AsSpan())` — без regex; или два `StartsWithSegments`. Выбрать вариант без аллокаций по возможности.

В `WebApiExceptionHandler.TryHandleAsync` заменить `IsInternalApiPath()` на `IsApiPath()` (строка 18). XML-doc класса: обновить — теперь он про API-пути, а не только про `/webapi/`.

- [ ] **Step 4: Запустить тесты**

Run: `dotnet test src/JoinRpg.Portal.Test --filter "FullyQualifiedName~WebApiExceptionHandlerTest"`
Expected: PASS (все, включая старые).

- [ ] **Step 5: Commit**

```bash
dotnet format
git add src/JoinRpg.Portal/Infrastructure/ApiPathHelper.cs src/JoinRpg.Portal/Infrastructure/WebApiExceptionHandler.cs src/JoinRpg.Portal.Test/Infrastructure/WebApiExceptionHandlerTest.cs
git commit -m "feat: ProblemDetails по исключениям для XApi-путей"
```

---

### Task 2: ProblemDetails для 401/403 авторизации (`XApiAuthorizationResultHandler`)

**Files:**
- Create: `src/JoinRpg.Portal/Infrastructure/XApi/XApiAuthorizationResultHandler.cs`
- Modify: `src/JoinRpg.Portal/Startup.cs:155-157` (регистрация в DI)
- Test: `src/JoinRpg.Portal.Test/Infrastructure/XApiAuthorizationResultHandlerTest.cs` (create)

**Interfaces:**
- Consumes: `ApiPathHelper.IsExternalApiPath(PathString)` (Task 1).
- Produces: `XApiAuthorizationResultHandler(IProblemDetailsService problemDetailsService)` — registered as singleton `IAuthorizationMiddlewareResultHandler`; заголовки: 403 → «Недостаточно прав для этого действия», 401 → «Токен доступа отсутствует, невалиден или истёк».

- [ ] **Step 1: Написать падающие юнит-тесты**

Новый файл, namespace `JoinRpg.Portal.Test.Infrastructure`. Образец контекста — из `WebApiExceptionHandlerTest.CreateHttpContext` (`ServiceCollection().AddLogging().AddProblemDetails()`), для challenge-теста добавить в сервисы `AddAuthenticationCore().AddAuthentication("Bearer").AddScheme<AuthenticationSchemeOptions, StubBearerHandler>("Bearer", _ => { })`, где `StubBearerHandler : AuthenticationHandler<AuthenticationSchemeOptions>` — `HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult())`, `HandleChallengeAsync()` ставит `Response.StatusCode = 401` и `Response.Headers.WWWAuthenticate = "Bearer"`.

Тесты:
1. `Forbid_OnXApiPath_ReturnsProblemDetails403` — контекст `/x-api/users/1/`, политика `new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme).RequireAuthenticatedUser().Build()`, вызов `HandleAsync(_ => Task.CompletedTask, context, policy, PolicyAuthorizationResult.Forbid())`. Ожидания: StatusCode 403, `Response.ContentType == "application/problem+json"`, JSON `title` == «Недостаточно прав для этого действия», `status` == 403.
2. `Challenge_OnXApiPath_ReturnsProblemDetails401AndKeepsAuthenticateHeader` — тот же путь, `PolicyAuthorizationResult.Challenge()`. Ожидания: 403 нет, StatusCode 401, `title` == «Токен доступа отсутствует, невалиден или истёк», заголовок `WWW-Authenticate` присутствует.
3. `Forbid_OnRegularPagePath_DelegatesToDefault` — путь `/1611/project/settings`. Ожидания: StatusCode 403, тело пустое, Content-Type не `application/problem+json`.
4. `Forbid_OnWebApiPath_DelegatesToDefault` — путь `/webapi/1611/project/SaveContactSettings`, поведение как в тесте 3 (поведение островов не меняется).

- [ ] **Step 2: Запустить, убедиться что падает**

Run: `dotnet test src/JoinRpg.Portal.Test --filter "FullyQualifiedName~XApiAuthorizationResultHandlerTest"`
Expected: FAIL — класса нет / тесты не компилируются.

- [ ] **Step 3: Реализовать `XApiAuthorizationResultHandler`**

В `src/JoinRpg.Portal/Infrastructure/XApi/XApiAuthorizationResultHandler.cs`, namespace `JoinRpg.Portal.Infrastructure.XApi`:

```csharp
internal class XApiAuthorizationResultHandler(IProblemDetailsService problemDetailsService)
    : IAuthorizationMiddlewareResultHandler
{
    private readonly DefaultAuthorizationMiddlewareResultHandler defaultHandler = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Succeeded || !context.Request.Path.IsExternalApiPath())
        {
            await defaultHandler.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        // Сначала дефолт: ставит код (и WWW-Authenticate при challenge), не пишет тело.
        await defaultHandler.HandleAsync(next, context, policy, authorizeResult);

        if (context.Response.HasStarted)
        {
            return;
        }

        var title = context.Response.StatusCode == StatusCodes.Status401Unauthorized
            ? "Токен доступа отсутствует, невалиден или истёк"
            : "Недостаточно прав для этого действия";
        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails { Status = context.Response.StatusCode, Title = title },
        });
    }
}
```

Регистрация в `Startup.cs` рядом со строками 155–157: `services.AddSingleton<IAuthorizationMiddlewareResultHandler, XApiAuthorizationResultHandler>();`.

- [ ] **Step 4: Запустить тесты**

Run: `dotnet test src/JoinRpg.Portal.Test --filter "FullyQualifiedName~XApiAuthorizationResultHandlerTest"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
dotnet format
git add src/JoinRpg.Portal/Infrastructure/XApi/XApiAuthorizationResultHandler.cs src/JoinRpg.Portal/Startup.cs src/JoinRpg.Portal.Test/Infrastructure/XApiAuthorizationResultHandlerTest.cs
git commit -m "feat: ProblemDetails для 401/403 в XApi"
```

---

### Task 3: Ветвление `UseStatusCodePages` для внешних API-путей

**Files:**
- Modify: `src/JoinRpg.Portal/Infrastructure/WebApiClientErrorTitles.cs`
- Modify: `src/JoinRpg.Portal/Startup.cs:209`
- Modify: `src/JoinRpg.Portal/Infrastructure/PortalExceptionHandling.cs` (по месту — см. Step 3)

**Interfaces:**
- Consumes: `ApiPathHelper.IsExternalApiPath(PathString)` (Task 1).
- Produces: `WebApiClientErrorTitles.GetTitle(int statusCode)` — русский заголовок по коду, fallback «Ошибка» для кода вне словаря. Поведение для не-API путей идентично прежнему `UseStatusCodePagesWithReExecute`.

- [ ] **Step 1: Написать падающий юнит-тест**

В `src/JoinRpg.Portal.Test/Infrastructure/WebApiClientErrorTitlesTest.cs` (создать рядом с существующим — посмотреть его содержимое и дописать):

```csharp
[Theory]
[InlineData(404, "Не найдено")]
[InlineData(405, "Метод не поддерживается")]
[InlineData(418, "Ошибка")]
public void GetTitle_ReturnsRussianTitleOrFallback(int status, string expected)
    => WebApiClientErrorTitles.GetTitle(status).ShouldBe(expected);
```

- [ ] **Step 2: Запустить, убедиться что падает**

Run: `dotnet test src/JoinRpg.Portal.Test --filter "FullyQualifiedName~WebApiClientErrorTitlesTest"`
Expected: FAIL — метода `GetTitle` нет.

- [ ] **Step 3: Реализовать**

1. В `WebApiClientErrorTitles` сделать словарь доступным: `internal static string GetTitle(int statusCode) => Titles.TryGetValue(statusCode, out var title) ? title : "Ошибка";`
2. В `Startup.cs` (метод `Configure`, строка 209) заменить `_ = app.UseStatusCodePagesWithReExecute(PortalExceptionHandling.ErrorPagePath + "/{0}");` на:

```csharp
_ = app.UseStatusCodePages(async context =>
{
    var httpContext = context.HttpContext;
    if (httpContext.Request.Path.IsExternalApiPath())
    {
        var status = httpContext.Response.StatusCode;
        await httpContext.RequestServices.GetRequiredService<IProblemDetailsService>()
            .TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails { Status = status, Title = WebApiClientErrorTitles.GetTitle(status) },
            });
        return;
    }

    httpContext.Request.Path = $"{PortalExceptionHandling.ErrorPagePath}/{httpContext.Response.StatusCode}";
    await context.Next(httpContext);
});
```

(`using` для `Microsoft.AspNetCore.Mvc` и `ProblemDetailsContext` — по месту.) Это копирует семантику `UseStatusCodePagesWithReExecute`: для не-API путей re-execute страницы ошибки; для внешнего API — ProblemDetails. `/webapi/` здесь не ветвится — остаётся прежний HTML, как сегодня.

- [ ] **Step 4: Запустить тесты + сборку**

Run: `dotnet test src/JoinRpg.Portal.Test --filter "FullyQualifiedName~WebApiClientErrorTitlesTest"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
dotnet format
git add src/JoinRpg.Portal/Infrastructure/WebApiClientErrorTitles.cs src/JoinRpg.Portal/Startup.cs src/JoinRpg.Portal.Test/Infrastructure/WebApiClientErrorTitlesTest.cs
git commit -m "feat: ProblemDetails для пустых 4xx в XApi вместо HTML-страницы"
```

---

### Task 4: Интеграционные тесты XApi

**Files:**
- Modify: `src/JoinRpg.IntegrationTest/TestInfrastructure/XApiClient.cs` (raw-методы, возвращающие `HttpResponseMessage`)
- Test: `src/JoinRpg.IntegrationTest/Scenarios/XApi/XApiProblemDetailsTests.cs` (create)

**Interfaces:**
- Consumes: всё из Task 1–3 (поведение приложения).
- Produces: `XApiClient.GetUserInfoMessageAsync(int userId)`, `XApiClient.GetFieldsMetadataMessageAsync(int projectId)` — возвращают `HttpResponseMessage` без `EnsureSuccessStatusCode`.

- [ ] **Step 1: Написать падающие интеграционные тесты**

Новый файл `XApiProblemDetailsTests.cs`, `[Collection("XApi")]`, конструктор `(XApiMasterFixture fixture)` — по образцу `XApiUserInfoTests`. Расширить `XApiClient` двумя message-методами (копия `GetUserInfoRawAsync`/`GetFieldsMetadataAsync` без `EnsureSuccessStatusCode`, возврат `HttpResponseMessage`).

Тесты (паттерн ассертов — из `WebApiRefusalScenario.cs:38-40`):

1. `MasterIsNotSiteAdmin_Returns403ProblemDetails` — `fixture.MasterClient.GetUserInfoMessageAsync(fixture.MasterUserId.Value)`: StatusCode 403, `Content.Headers.ContentType.MediaType == "application/problem+json"`, тело содержит «Недостаточно прав для этого действия».
2. `WithoutToken_Returns401ProblemDetails` — `fixture.AnonymousXApiClient.GetUserInfoMessageAsync(...)`: StatusCode 401, problem+json, тело содержит «Токен доступа отсутствует, невалиден или истёк», заголовок `WWW-Authenticate` присутствует.
3. `PlayerIsNotMaster_Returns403ProblemDetails` — создать игрока: `var (playerId, _) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(fixture.Factory.Services);` залогиниться: `var playerClient = await XApiClient.CreateXApiClient(fixture.Factory.CreateClient(), email, password)` (email/password — из возврата хелпера; посмотреть сигнатуру `CreateTestUserWithEmailAsync`), вызвать `GetFieldsMetadataMessageAsync(fixture.ProjectId.Value)` → 403 + problem+json.
4. `ProjectNotFound_Returns404ProblemDetails` — `fixture.MasterClient.GetFieldsMetadataMessageAsync(int.MaxValue)` → 404 + problem+json + «Проект не найден». (Если контроллер маппит это иначе — зафиксировать фактический код в отчёте, не подгонять под желаемый.)
5. `UnknownEndpoint_Returns404ProblemDetails` — `fixture.AnonymousXApiClient`… нет: `fixture.MasterClient`, `httpClient.GetAsync($"/x-game-api/{fixture.ProjectId.Value}/no-such-endpoint")` напрямую через `Factory.CreateClient()` с токеном мастера (можно взять `fixture.MasterClient` внутренний HttpClient недоступен — добавить в `XApiClient` общий метод `GetRawAsync(string path)`) → 404 + problem+json + «Не найдено».

- [ ] **Step 2: Запустить, убедиться что падают**

Run: `dotnet test src/JoinRpg.IntegrationTest --filter "FullyQualifiedName~XApiProblemDetailsTests"`
Expected: FAIL — тесты 1–3: Content-Type не problem+json (пустое тело/HTML); тесты 4–5 по факту.

- [ ] **Step 3: Финальная проверка и доработка**

Run: `dotnet test src/JoinRpg.IntegrationTest --filter "FullyQualifiedName~XApiProblemDetailsTests"`
Expected: PASS.
Затем прогнать соседние XApi-тесты на регрессию кодов (они проверяют только статусы):
Run: `dotnet test src/JoinRpg.IntegrationTest --filter "FullyQualifiedName~XApiUserInfoTests|FullyQualifiedName~XApiTokenTests"`
Expected: PASS.

- [ ] **Step 4: Commit**

```bash
dotnet format
git add src/JoinRpg.IntegrationTest/TestInfrastructure/XApiClient.cs src/JoinRpg.IntegrationTest/Scenarios/XApi/XApiProblemDetailsTests.cs
git commit -m "test: ProblemDetails в ответах ошибок XApi"
```

---

### Task 5: Финальная верификация

- [ ] **Step 1: Полная сборка и юнит-тесты Portal**

```bash
dotnet build
dotnet test src/JoinRpg.Portal.Test
```

Expected: PASS без ошибок.

- [ ] **Step 2: Все XApi-интеграционные тесты**

Run: `dotnet test src/JoinRpg.IntegrationTest --filter "FullyQualifiedName~Scenarios.XApi"`
Expected: PASS (изменены общие обработчики — прогоняем весь XApi-сюит локально, это оправдано правкой самих тестов, см. Global Constraints).

- [ ] **Step 3: Проверить затронутые смежные тесты**

Run: `dotnet test src/JoinRpg.Portal.Test --filter "FullyQualifiedName~PscbPaymentsControllerTest"`
Expected: PASS (контроллер под `/x-api` — его тесты могли зависеть от тел ошибок).
