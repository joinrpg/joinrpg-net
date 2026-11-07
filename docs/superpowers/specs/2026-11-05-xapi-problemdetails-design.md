# ProblemDetails для XApi

Дата: 2026-11-05
Статус: на утверждении

## Зачем

XApi (`/x-api`, `/x-game-api`) — внешнее API на JWT. Сейчас его ошибки непригодны для машинных клиентов:

- 403 при отсутствии мастерского доступа — пустое тело, поверх `UseStatusCodePagesWithReExecute` подставляется HTML-страница `/error/403` (код сохраняется, тело — HTML);
- 401 от JWT challenge — пустое тело;
- доменные отказы (`NoAccessToProjectException` и др.) → HTML-страница `/error` со статусом 500 вместо ProblemDetails 403/404.

Для `/webapi/` (острова Blazor) ProblemDetails уже отдаётся: `WebApiExceptionHandler` + русские заголовки из `WebApiClientErrorTitles` (`src/JoinRpg.Portal/Infrastructure/`). Задача — распространить этот формат на XApi.

## Цели

- Все ошибки XApi (401, 403, 404, 405, доменные отказы, 500) — JSON ProblemDetails (RFC 9457), без HTML.
- Статус-коды не меняются — существующие клиенты, проверяющие только код, не ломаются.

## Не-цели

- Поведение `/webapi/` не меняется (оно уже в ProblemDetails и уходит в релиз как есть).
- Автоматическая локализация под клиента — заголовки на русском, как в `WebApiClientErrorTitles` (прецедент; XApi-клиенты машинные и читают `status`, но формат держим единым).
- Обработка тел успешных ответов, Swagger-документация ошибок — отдельная задача, если понадобится.

## Подходы (обсуждено)

1. **Расширить существующий механизм на внешние API — выбран.** Переиспользует `WebApiExceptionHandler` и русские заголовки; единый путь для исключений и ошибок авторизации.
2. Только события JwtBearer (`OnChallenge`/`OnForbidden`) — закрывает лишь 401/403, исключения остаются HTML. Отклонён: не закрывает охват «все ошибки».
3. Встроенный `MapToApiProblemDetails()` (.NET 10) — не покрывает исключения (нужен вариант 1 в любом случае), критерий «это API» по Accept даёт два разных ответа на один запрос. Отклонён как дублирующий.

## Как строим

### 1. Расширить `ApiPathHelper`

Добавить `IsApiPath(this PathString)` = `/webapi` (как сейчас `IsInternalApiPath`) или `/x-api` / `/x-game-api` (сегментная проверка `IsApiPathSegment` уже есть). `IsInternalApiPath` оставить для cookie-специфики (`AuthenticationConfigurator`, `RedirectAntiforgeryValidationFailedResultFilter`).

### 2. Исключения: `WebApiExceptionHandler`

Условие `GetOriginalPath(...).IsInternalApiPath()` заменить на `IsApiPath()`. Доменные отказы (`NoAccessToProjectException` → 403 «Недостаточно прав для этого действия» и т.д.) начнут уходить в XApi как ProblemDetails с теми же кодами и заголовками.

### 3. Авторизация: новый `XApiAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler`

Обертка над `DefaultAuthorizationMiddlewareResultHandler`:

- путь не внешний API (`!IsExternalApiPath`) — делегируем дефолту без изменений;
- путь внешний API и результат — `Forbid` → ProblemDetails 403, заголовок «Недостаточно прав для этого действия» (из общего словаря заголовков);
- путь внешний API и результат — `Challenge` → ProblemDetails 401 с сохранением заголовка `WWW-Authenticate` от схемы (обработать через `AuthenticateAsync` дефолтной схемы, как делает дефолтный хендлер), заголовок вида «Токен доступа отсутствует, невалиден или истёк».

Регистрация в DI поверх дефолтного (`services.AddSingleton<IAuthorizationMiddlewareResultHandler, XApiAuthorizationResultHandler>` в `Startup.cs`, рядом с `AddProblemDetails()`).

### 4. Прочие пустые 4xx: `UseStatusCodePages` с ветвлением

Сейчас `UseStatusCodePagesWithReExecute` для любого пустого 4xx рисует HTML `/error/{0}`. Заменить на `UseStatusCodePages` с делегатом: если путь `IsApiPath` → ProblemDetails с заголовком из словаря `WebApiClientErrorTitles` по фактическому коду; иначе — re-execute страницы ошибки как раньше. Покрывает 404 несовпавшего маршрута, 405 и прочие «пустые» отказы.

Пункты 3–4 пишут тело ответа, поэтому больше не задеваются StatusCodePages-редиректом.

### 5. Тесты

- Интеграционные (расширить `XApiUserInfoTests`, `XApiTokenTests`, добавить в XApi-сценарии):
  - мастер без прав админа на `x-api/users` → 403 + тело ProblemDetails (`title`, `status`);
  - игрок (не мастер) на `x-game-api/{projectId}/...` → 403 + ProblemDetails;
  - запрос без токена → 401 + ProblemDetails + наличие `WWW-Authenticate`;
  - доменный отказ (например, архивный проект) → 409/404 + ProblemDetails, а не HTML;
  - несуществующий маршрут `x-game-api/...` → 404 + ProblemDetails, а не HTML.
- Юнит (`JoinRpg.Portal.Test`, по образцу `WebApiExceptionHandlerTest`): хендлер авторизации — внешний путь пишет ProblemDetails, внутренний делегирует дефолту; ветвление StatusCodePages.

Существующие тесты, сверяющие только код (`Forbidden` в `XApiUserInfoTests.cs` и др.), должны продолжать проходить — коды не меняются.

## Мелкие решения (за реализатора)

- Заголовок для 401 в XApi формулируем отдельно от cookie-варианта («обновите страницу» к JWT неприменимо) — отдельная запись в словаре или параметризация.
- Точное место общего словаря заголовков: расширить `WebApiClientErrorTitles` публичным доступом по коду или вынести словарь — по месту.
- Проверить `PscbPaymentsApiControllerTest` и другие тесты Portal, реагирующие на тела ошибок.

## Риски

- Внешние клиенты XApi, парсившие HTML тела ошибок (маловероятно, но возможно) увидят JSON. Коды не меняются — совместимость по коду сохраняется.
- Двойное письмо тела, если обработчик авторизации и StatusCodePages оба сработают: предотвращается проверкой `response.HasStarted`/наличия тела и порядком middleware (StatusCodePages — последним).
