# Руководство по созданию страниц редактирования

Страница редактирования (CaptainRules, ResponsibleMasterRules, управление сетками ролей) — Blazor-«остров» с возможностью добавлять/изменять/удалять данные проекта.

Общие шаги (стартовые решения, слои, паттерн Builder, тесты, ViewModel, Razor-страница, навигация, авторизация, правила кода) описаны в [ui-pages-common.md](ui-pages-common.md). Здесь — только **специфика редактирования**. Для read-only страниц см [view-pages-guide.md](view-pages-guide.md).

## Что добавляется по сравнению с общими шагами

1. **Сервис с мутациями** (`JoinRpg.Services`) — бизнес-логика создания/изменения/удаления с проверкой прав.
2. **POST-эндпоинты** в WebAPI-контроллере (помимо GET).
3. **Компоненты формы и строки** (`*Form.razor`, `*Row.razor`) с оптимистичным обновлением и откатом.

## 1. Сервис с мутациями

```csharp
// JoinRpg.Services.Interfaces/ProjectMetadata/IProjectRolesListService.cs
public interface IProjectRolesListService
{
    Task CreateAsync(ProjectRolesList model);
    Task UpdateAsync(ProjectRolesList model);
    Task RemoveAsync(ProjectRolesListIdentification id);
}
```

Реализация в `JoinRpg.Services.Impl` (проверка прав внутри, `RequestMasterAccess`), регистрируется в Autofac (`JoinRpg.Services.Impl/Module.cs`).

## 2. Web-клиент: методы чтения + мутаций

```csharp
public interface IProjectRolesListClient
{
    Task<ProjectRolesListViewModel> GetList(ProjectIdentification projectId);
    Task<ProjectRolesListViewModel> Create(ProjectIdentification projectId, AddProjectRolesListViewModel model);
    Task<ProjectRolesListViewModel> Update(ProjectRolesList model);
    Task Remove(ProjectRolesListIdentification id);
}
```

ViewModel'и включают модель формы ввода с атрибутами валидации:

```csharp
public class AddProjectRolesListViewModel
{
    [Required(ErrorMessage = "Укажите название")]
    [Display(Name = "Название")]
    public string Name { get; set; } = "";

    public ProjectRolesList ToDomain(ProjectIdentification projectId, int temporaryId = -1) => /* ... */;
}
```

## 3. WebAPI: GET + POST

POST-методы закрываются нужным Permission и проверяют принадлежность к проекту. Класс помечается `[IgnoreAntiforgeryToken]` (CSRF проверяется через токен в куке, см [ADR001](adr001-blazor.md)).

```csharp
[Route("/webapi/project-roles-list/[action]")]
[RequireMaster]
[IgnoreAntiforgeryToken]
public class ProjectRolesListController(IProjectRolesListClient client) : ControllerBase
{
    [HttpGet]
    public async Task<ProjectRolesListViewModel> GetList(ProjectIdentification projectId)
        => await client.GetList(projectId);

    [HttpPost]
    [RequireMaster(Permission.CanEditRoles)]
    public async Task<ActionResult> Remove([FromQuery] ProjectIdentification projectId, [FromBody] ProjectRolesListIdentification id)
    {
        if (id.ProjectId != projectId) { return BadRequest(); }
        await client.Remove(id);
        return Ok();
    }

    [HttpPost]
    [RequireMaster(Permission.CanEditRoles)]
    public async Task<ActionResult<ProjectRolesListViewModel>> Create([FromQuery] ProjectIdentification projectId, [FromBody] AddProjectRolesListViewModel model)
        => Ok(await client.Create(projectId, model));
}
```

## 4. HTTP-клиент: мутации с CSRF-токеном

Перед POST вызывается `csrfTokenProvider.SetCsrfToken(httpClient)`:

```csharp
public async Task<ProjectRolesListViewModel> Create(ProjectIdentification projectId, AddProjectRolesListViewModel model)
{
    await csrfTokenProvider.SetCsrfToken(httpClient);
    var response = await httpClient.PostAsJsonAsync($"webapi/project-roles-list/create?projectId={projectId.Value}", model);
    return await response.EnsureSuccessStatusCode().Content
        .ReadFromJsonAsync<ProjectRolesListViewModel>() ?? throw new Exception("Empty");
}
```

## 5. Blazor-компоненты: список + форма + строка с откатом

- `*Grid.razor` — список (например, `JoinPanelWithList`), кнопка добавления (disabled без прав).
- `*Form.razor` — форма добавления/редактирования.
- `*Row.razor` — строка с кнопкой удаления.

**Оптимистичное обновление + откат**: операция сразу меняет локальную модель, при ошибке сети откатывается.

```csharp
async Task OnRemoveRow(ItemViewModel item)
{
    var idx = _model!.Items.IndexOf(item);
    _model.Items.RemoveAt(idx);
    try { await Client.Remove(item.Id); }
    catch (Exception exc)
    {
        logger.LogError(exc, "Ошибка при удалении");
        _model.Items.Insert(idx, item); // откат
    }
}
```

## bUnit-тесты компонентов

Для компонентов с зависимостями:

```csharp
var mockClient = new Mock<IProjectRolesListClient>();
mockClient.Setup(c => c.GetList(It.IsAny<ProjectIdentification>()))
    .ReturnsAsync(new ProjectRolesListViewModel(Items: [], HasEditAccess: false));
ctx.Services.AddSingleton(mockClient.Object);
```

## Типичные проблемы

- **Обработка ошибок**: компоненты должны обрабатывать ошибки сети и откатывать локальные изменения при неудачных операциях.
- Остальное (авторизация, маршрутизация, Blazor-режим) — см [ui-pages-common.md](ui-pages-common.md).

## Примеры для изучения

- `CaptainRules` — полный пример с CRUD.
- `ResponsibleMasterRules` — аналогичная структура.
- `ProjectRolesList` — управление сетками ролей.
- `Subscribe` — более сложный пример с подписками.
