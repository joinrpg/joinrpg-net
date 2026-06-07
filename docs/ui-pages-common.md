# Общие шаги для UI-страниц проекта (Blazor-islands)

Этот документ описывает части, общие для **страниц редактирования** ([editing-pages-guide.md](editing-pages-guide.md)) и **страниц просмотра** ([view-pages-guide.md](view-pages-guide.md)). Оба гайда ссылаются сюда, чтобы не дублировать инструкции.

Базовая архитектура «островов» Blazor описана в [ADR001](adr001-blazor.md).

## Стартовые решения

Прежде чем писать код (если ты агент — подтверди свои предположения у пользователя):

1. **Аудитория страницы** — кто её видит. От этого зависит выбор UI-проекта (см. ниже) и авторизация.
2. **UI-проект**, в котором живёт реализация. См [structure.md](structure.md) и раздел «Выбор UI-проекта» ниже. **`JoinRpg.Web.ProjectMasterTools` — только для мастерских инструментов** (настроечные страницы, не видные игроку). Страницы, видимые игроку, кладём в другой профильный `JoinRpg.Web.*` проект.
3. **Permission (роль)**, за которой закрыт доступ к странице (для игроцких страниц это может быть не `[RequireMaster]`, а проверка видимости/публичности).
4. **Место в меню** (мастерском/игроцком), где будет ссылка, и её название. См `JoinRpg.Portal/Views/Shared/Components/ProjectMenu/MasterMenu.cshtml`.

### Выбор UI-проекта

UI-проекты в `WebPortalShared` разделены по фичам/аудитории (см [structure.md](structure.md)). Класть реализацию надо в проект, соответствующий **аудитории и теме** страницы:

> Управление и отображение одной фичи могут жить в **разных** проектах: настройка (мастерская) — в `ProjectMasterTools`, а отображение (игроцкое) — в `ProjectCommon`/профильном проекте. У них тогда отдельные `I*Client` и ViewModel'и.

## Слои (общие для обоих типов страниц)

1. **DataModel** (`JoinRpg.DataModel`) — EF-сущность для хранения в БД (см [project-entities.md](project-entities.md) для метаданных проекта).
2. **Репозиторий** (`JoinRpg.Data.Interfaces` + реализация в `JoinRpg.Dal.Impl.Repositories`) — доступ к данным; регистрируется в `JoinRpg.Dal.Impl/Module.cs`.
3. **Web-клиент** (профильный `JoinRpg.Web.*` проект — см «Выбор UI-проекта») — интерфейс `I*Client` для Blazor-компонентов и ViewModel'и.
4. **Серверная реализация клиента** (`JoinRpg.WebPortal.Managers`) — класс, реализующий `I*Client` через репозитории/сервисы; регистрируется в `JoinRpg.WebPortal.Managers.Registration.cs`.
5. **Клиентская реализация (HTTP)** (`JoinRpg.Blazor.Client`) — реализация `I*Client` через `HttpClient`; регистрируется в `JoinRpg.Blazor.Client.ApiClients.HttpClientRegistration.cs`.
6. **WebAPI-контроллер** (`JoinRpg.Portal.Controllers.WebApi`) — маршрут `/webapi/{feature}/[action]`, авторизация по аудитории страницы (`[RequireMaster]` для мастерских).
7. **Blazor-компоненты** (тот же профильный `JoinRpg.Web.*` проект, что и Web-клиент).
8. **Razor-страница** (`JoinRpg.Portal/Pages/GamePages`) — минимальная страница, рендерящая Blazor-компонент. Маршрут вида `/{projectId:int}/...`.
9. **Навигация** — ссылка в меню проекта.

> Чем именно наполняются слои 6–9, зависит от типа страницы — см. соответствующий гайд (редактирование/просмотр).

## Паттерн Builder (Domain → ViewModel)

Преобразование доменных объектов в ViewModel выделяется в **отдельный статический Builder-класс**. Это ключ к тестируемости: проверка преобразования данных без зависимостей от репозиториев.

```csharp
// JoinRpg.WebPortal.Managers/<Feature>/<Feature>ViewModelBuilder.cs
internal static class FeatureViewModelBuilder
{
    public static FeatureViewModel Build(
        DomainModel domainModel,
        ProjectInfo projectInfo,
        ICurrentUserAccessor currentUserAccessor)
    {
        var hasEditAccess = projectInfo.HasMasterAccess(
            currentUserAccessor.UserIdentification,
            Permission.CanManageClaims);
        // ...
    }
}
```

`ICurrentUserAccessor` передаётся в Builder только если результат зависит от прав/личности пользователя (например, флаг `HasEditAccess`). Если страница доступна только под фиксированной ролью и результат не зависит от пользователя — Builder можно делать без него (проще и тестируемее).

### Тесты Builder

Тесты — в `JoinRpg.WebPortal.Managers.Test/ProjectMasterTools/`. Использовать фейковый `ICurrentUserAccessor` (без Moq/NSubstitute — в проекте классические юнит-тесты):

```csharp
internal record FakeCurrentUserAccessor(UserIdentification? UserIdentification) : ICurrentUserAccessor
{
    public UserIdentification? UserIdentificationOrDefault => UserIdentification;
    public UserIdentification UserIdentification => UserIdentification
        ?? throw new InvalidOperationException("User not authenticated");
}
```

## Соглашения по ViewModel

- ViewModel'и пересекают WASM-границу и сериализуются в JSON — это **простые сериализуемые `record`'ы** (без HTML-строк, делегатов, ссылок на EF-сущности). Храните value-Id (`*Identification`), а не доменные объекты.
- Избегайте полиморфизма в сериализуемых моделях — при необходимости используйте поле-дискриминатор (`enum Kind`).

## Razor-страница

Минимальная страница в `JoinRpg.Portal/Pages/GamePages/`, рендерящая компонент:

```razor
@page "/{projectId:int}/tools/<feature-name>"
@using JoinRpg.Web.ProjectMasterTools.<Feature>;
@model JoinRpg.Portal.Pages.GamePages.<Feature>Model

@(await Html.RenderComponentAsync<FeatureComponent>(RenderMode.WebAssembly, new { Model.ProjectId }))
```

```csharp
[RequireMaster]
public class FeatureModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public required ProjectIdentification ProjectId { get; set; }

    public void OnGet() { }
}
```

## Навигация

Ссылка в `JoinRpg.Portal/Views/Shared/Components/ProjectMenu/MasterMenu.cshtml` (раздел выбрать согласно стартовому решению), с проверкой прав:

```html
<li>
    <a asp-page="/GamePages/FeaturePage" asp-route-projectId="@Model.ProjectId">Название</a>
</li>
```

## Авторизация и типичные проблемы

- **Авторизация**: `[RequireMaster]` / `[RequireProjectMaster]` в зависимости от требуемых прав.
- **Маршрутизация**: убедиться, что путь уникален и не конфликтует с существующими.
- **Blazor-режим**: `RenderMode.WebAssembly` или `RenderMode.WebAssemblyPrerendered` в зависимости от необходимости пререндеринга.

## Правила кода

- Русский язык UI; видимые пользователю строки только в `JoinRpg.Portal` и `JoinRpg.Services.Email`, в остальных проектах — `TODO[Localize]` при нарушении (см [structure.md](structure.md)).
- **Builder выделять отдельно** — для тестируемости преобразования без репозиториев.

## Примеры для изучения

- `CaptainRules` — полный пример редактирования (CRUD).
- `ResponsibleMasterRules` — аналогичная структура.
- `ProjectRolesList` / `ProjectRoleGrid` — управление + просмотр сетки ролей ([ADR008](adr008-roles-list-view-page.md)).
- `Subscribe` — более сложный пример с подписками.
