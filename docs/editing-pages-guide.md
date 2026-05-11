# Руководство по созданию страниц редактирования

Для создания страниц проекта (CaptainRules, ResponsibleMasterRules) можно использовать следующий паттерн. Что надо выбрать (если ты агент — спроси у пользователя подтверждение своих предположений):
1. Для начала надо выбрать UI проект, в котором поместить реализацию. См [structure.md]. Для метаданных проекта, например, хорошо подойдет проект `JoinRpg.Web.ProjectMasterTool`.
1. Нужно выбрать роль (Permission), за которой будет закрыт доступ к странице.
1. Место в мастерском/игроцком меню, где он будет находиться. См `JoinRpg.Portal/Views/Shared/Components/ProjectMenu/MasterMenu.cshtml` и как будет называться.

## Структура

1. **DataModel** (`JoinRpg.DataModel`)
   - Сущность, которая будет храниться в базе данных

2. **Репозиторий** (`JoinRpg.Data.Interfaces`)
   - Интерфейс репозитория для доступа к данным (опционально, если нужны специфичные запросы)
   - Реализация в `JoinRpg.Dal.Impl.Repositories`

3. **Сервисный слой** (`JoinRpg.Services`)
   - Интерфейс сервиса в `JoinRpg.Services.Interfaces`
   - Реализация в `JoinRpg.Services.Impl`
   - Пример: `ICaptainRuleService`, `IResponsibleMasterRuleService`

4. **Web-клиент** (`JoinRpg.Web.ProjectMasterTools`)
   - Интерфейс клиента (`I*Client`) для Blazor-компонентов
   - ViewModel'и для отображения и редактирования

5. **Серверная реализация клиента** (`JoinRpg.WebPortal.Managers`)
   - Класс, реализующий `I*Client`, работающий с репозиториями и сервисами
   - Регистрируется в `JoinRpg.WebPortal.Managers.Registration.cs`

6. **Клиентская реализация (HTTP)** (`JoinRpg.Blazor.Client`)
   - Реализация для Blazor WebAssembly, использующая `HttpClient`
   - Регистрируется в `JoinRpg.Blazor.Client.ApiClients.HttpClientRegistration.cs`

7. **WebAPI контроллер** (`JoinRpg.Portal.Controllers.WebApi`)
   - Контроллер с маршрутом `/webapi/{feature}/[action]`
   - Использует серверную реализацию клиента
   - Настроена авторизация (`[RequireMaster]`)

8. **Blazor-компоненты** (`JoinRpg.Web.ProjectMasterTools`)
   - Компонент списка (например, `*List.razor`)
   - Компонент формы (например, `*Form.razor`)
   - Компонент строки (например, `*Row.razor`)

9. **Razor-страница** (`JoinRpg.Portal/Pages/GamePages`)
   - Минимальная страница с рендерингом Blazor-компонента
   - Модель страницы (code-behind) с параметром `ProjectId`
   - Маршрут: `/{projectId:int}/tools/{feature-name}`

10. **Навигация**
    - Добавление ссылки в меню проекта (`JoinRpg.Portal/Views/Shared/Components/ProjectMenu/MasterMenu.cshtml`)
    - Проверка прав доступа (`Permission.CanManageClaims` или другие)

## Шаблонные шаги

### 1. Создание репозитория (если нужно)

```csharp
// JoinRpg.Data.Interfaces/ProjectMetadata/IProjectRolesListRepository.cs
public interface IProjectRolesListRepository
{
    Task<ProjectRolesList?> GetForProjectAsync(int projectId);
    Task AddOrUpdateAsync(ProjectRolesList entity);
    Task RemoveAsync(ProjectRolesList entity);
}
```

Реализация в `JoinRpg.Dal.Impl.Repositories/ProjectRolesListRepository.cs`. Зарегистрировать в `JoinRpg.Dal.Impl/Module.cs`.

### 2. Создание сервиса

```csharp
// JoinRpg.Services.Interfaces/ProjectMetadata/IProjectRolesListService.cs
public interface IProjectRolesListService
{
    Task<ProjectRolesList> GetForProjectAsync(int projectId);
    Task<ProjectRolesList> AddOrUpdateAsync(ProjectRolesList model);
    Task RemoveAsync(int projectId);
}
```

```csharp
// JoinRpg.Services.Impl/ProjectMetadata/ProjectRolesListService.cs
public class ProjectRolesListService(IUnitOfWork unitOfWork) : IProjectRolesListService
{
    // Реализация
}
```

Зарегистрировать в Autofac (`JoinRpg.Services.Impl/Module.cs`).

### 3. Создание Web-клиента и ViewModel'ов

```csharp
// JoinRpg.Web.ProjectMasterTools/ProjectRolesList/IProjectRolesListClient.cs
public interface IProjectRolesListClient
{
    Task<ProjectRolesListViewModel> GetList(ProjectIdentification projectId);
    Task<ProjectRolesListViewModel> AddOrChange(ProjectRolesListViewModel model);
    Task Remove(ProjectRolesListIdentification id);
}

// JoinRpg.Web.ProjectMasterTools/ProjectRolesList/ViewModels.cs
public record ProjectRolesListViewModel(
    IReadOnlyList<ProjectRolesListItemViewModel> Items,
    bool HasEditAccess);

public record ProjectRolesListItemViewModel(
    ProjectRolesListIdentification Id,
    // ... поля
);

public record AddProjectRolesListViewModel(
    // ... поля формы
);
```

### 4. Создание Builder для преобразования Domain → ViewModel

```csharp
// JoinRpg.WebPortal.Managers/ProjectMasterTools/ProjectRolesList/ProjectRolesListViewModelBuilder.cs
internal static class ProjectRolesListViewModelBuilder
{
    public static ProjectRolesListViewModel Build(
        ProjectRolesList domainModel,
        ProjectInfo projectInfo,
        ICurrentUserAccessor currentUserAccessor)
    {
        var items = domainModel.Roles.Select(role => 
            new ProjectRolesListItemViewModel(
                Id: new ProjectRolesListIdentification(role.Id),
                // ... другие поля
            )).ToList();

        return new ProjectRolesListViewModel(
            Items: items,
            HasEditAccess: projectInfo.HasMasterAccess(
                currentUserAccessor.UserIdentification, 
                Permission.CanManageClaims)
        );
    }
}
```

**Тесты для Builder**: создать в `JoinRpg.WebPortal.Managers.Test/ProjectMasterTools/ProjectRolesListViewModelBuilderTests.cs`. Использовать фейковый `ICurrentUserAccessor`.

### 5. Создание серверной реализации клиента

```csharp
// JoinRpg.WebPortal.Managers/ProjectMasterTools/ProjectRolesList/ProjectRolesListViewService.cs
internal class ProjectRolesListViewService : IProjectRolesListClient
{
    // Использует репозитории, сервисы, ICurrentUserAccessor
    // Использует Builder для преобразования
}
```

Зарегистрировать в `JoinRpg.WebPortal.Managers.Registration.cs`:
```csharp
services.AddScoped<IProjectRolesListClient, ProjectRolesListViewService>();
```

### 5. Создание клиентской реализации (HTTP)

```csharp
// JoinRpg.Blazor.Client/ApiClients/ProjectRolesListClient.cs
internal class ProjectRolesListClient(HttpClient httpClient): IProjectRolesListClient
{
    // Реализация методов через вызовы WebAPI
}
```

Зарегистрировать в `JoinRpg.Blazor.Client.ApiClients.HttpClientRegistration.cs`:
```csharp
builder.AddHttpClient<IProjectRolesListClient, ProjectRolesListClient>();
```

### 6. Создание WebAPI контроллера

```csharp
// JoinRpg.Portal/Controllers/WebApi/ProjectRolesListController.cs
[Route("/webapi/project-roles-list/[action]")]
[RequireMaster]
[IgnoreAntiforgeryToken]
public class ProjectRolesListController(IProjectRolesListClient client) : ControllerBase
{
    [HttpGet]
    public async Task<ProjectRolesListViewModel> GetList(ProjectIdentification projectId)
        => await client.GetList(projectId);

    [HttpPost]
    [RequireMaster(Permission.CanManageClaims)]
    public async Task<ActionResult> Remove([FromQuery] ProjectIdentification projectId, [FromBody] ProjectRolesListIdentification id)
    {
        // Проверка принадлежности к проекту
        await client.Remove(id);
        return Ok();
    }

    [HttpPost]
    [RequireMaster(Permission.CanManageClaims)]
    public async Task<ActionResult<ProjectRolesListViewModel>> AddOrChange([FromQuery] ProjectIdentification projectId, [FromBody] ProjectRolesListViewModel model)
    {
        // Проверка
        return Ok(await client.AddOrChange(model));
    }
}
```

### 7. Создание Blazor-компонентов

- `ProjectRolesList.razor` — основной компонент списка (аналог `CaptainRulesList.razor`)
- `ProjectRolesListForm.razor` — форма добавления/редактирования
- `ProjectRolesListRow.razor` — строка в списке

Использовать существующие компоненты UI (`JoinPanelWithList`, `JoinButton`, `JoinLoadingMessage`).

### 8. Создание Razor-страницы

```razor
@page "/{projectId:int}/tools/roles-list"
@using JoinRpg.Web.ProjectMasterTools.ProjectRolesList;

@model JoinRpg.Portal.Pages.GamePages.ProjectRolesListModel

@{
    ViewData["Title"] = "Список ролей проекта";
}

<h3>@ViewData["Title"]</h3>

@(await Html.RenderComponentAsync<ProjectRolesList>(RenderMode.WebAssembly, new {Model.ProjectId}))
```

```csharp
// ProjectRolesList.cshtml.cs
[RequireMaster]
public class ProjectRolesListModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public required ProjectIdentification ProjectId { get; set; }

    public void OnGet() { }
}
```

### 9. Добавление ссылки в меню проекта

В файл `JoinRpg.Portal/Views/Shared/Components/ProjectMenu/MasterMenu.cshtml` добавить (раздел выбрать согласно указанию пользователя в начале):

```html
<li>
    <a asp-page="/GamePages/ProjectRolesList" asp-route-projectId="@Model.ProjectId">
        Список ролей проекта
    </a>
</li>
```

## Типичные проблемы

1. **Авторизация**: Использовать `[RequireMaster]` или `[RequireProjectMaster]` в зависимости от требуемых прав.
2. **Маршрутизация**: Убедиться, что путь уникален и не конфликтует с существующими.
3. **Blazor-режим**: Использовать `RenderMode.WebAssembly` или `RenderMode.WebAssemblyPrerendered` в зависимости от необходимости предварительного рендеринга.
4. **Обработка ошибок**: Компоненты должны обрабатывать ошибки сети и откатывать локальные изменения при неудачных операций.

## Инструменты тестирования

### Фейковый ICurrentUserAccessor для тестов Builder

```csharp
// JoinRpg.WebPortal.Managers.Test/TestDoubles/FakeCurrentUserAccessor.cs
internal record FakeCurrentUserAccessor(UserIdentification? UserIdentification) : ICurrentUserAccessor
{
    public UserIdentification? UserIdentificationOrDefault => UserIdentification;
    public UserIdentification UserIdentification => UserIdentification 
        ?? throw new InvalidOperationException("User not authenticated");
}
```

Использование в тестах:
```csharp
var fakeAccessor = new FakeCurrentUserAccessor(new UserIdentification(1));
var result = ProjectRolesListViewModelBuilder.Build(domainModel, projectInfo, fakeAccessor);
```

### Моки для bUnit тестов

Для тестирования компонентов с зависимостями:
```csharp
var mockClient = new Mock<IProjectRolesListClient>();
mockClient.Setup(c => c.GetList(It.IsAny<ProjectIdentification>()))
    .ReturnsAsync(new ProjectRolesListViewModel(Items: [], HasEditAccess: false));

ctx.Services.AddSingleton(mockClient.Object);
```

## Примеры для изучения

- `CaptainRules` — полный пример с CRUD
- `ResponsibleMasterRules` — аналогичная структура
- `Subscribe` — более сложный пример с подписками


### Примечания
- Следовать стилю кода проекта (русский язык UI, `TODO[Localize]` если нужно)
- Использовать существующие паттерны из `CaptainRules` и `ResponsibleMasterRules`
- Проверить работу в режиме Blazor WebAssembly
- **Builder должен быть выделен для тестируемости** — это упрощает проверку преобразования данных без зависимостей от репозиториев
