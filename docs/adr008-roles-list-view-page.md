# ADR008: Страница просмотра сетки ролей (ProjectRoleGrid)

Проблема:
==

`ProjectRolesList` (`JoinRpg.DomainTypes/ProjectMetadata/ProjectRolesList.cs`) — это **настройка** страницы «сетка ролей»: от какой группы строить (`CharacterGroupId`, `null` = от верха), какие поля показывать колонками (`Fields`), показывать ли колонку контактов / интересных групп (`ContactsColumn`, `GroupsColumn` с режимами `None / PublicOnly / All`).

Управление списком этих настроек — мастерская функция, уже реализована в `JoinRpg.Web.ProjectMasterTools` (страница `/{projectId}/tools/roleslist`, компонент `ProjectRolesListGrid`). А вот сама страница **отображения** конкретной сетки — `src/JoinRpg.Portal/Pages/GamePages/ProjectRoleListView.cshtml` (роут `/{projectId:int}/roleslist/{id:int}`) — сейчас заглушка `TODO`. Code-behind уже готов: содержит `RolesListId` (`ProjectRolesListIdentification`).

Отображение сетки ролей — **игроцкая** функция: у настройки есть флаг `PublicMode` («Доступна ли эта сетка ролей публично, или только через вводные»). Если `PublicMode == true` — сетку видит любой пользователь; если `false` — только мастер. Поэтому её код **не должен** жить в `ProjectMasterTools` (инструменты мастера). Нужно отрисовать таблицу персонажей — то же, что делает `JoinrpgMarkdownLinkRenderer.ExperimentalTableFunc` (`src/JoinRpg.WebPortal.Models/Helpers/JoinrpgMarkdownLinkRenderer.cs`), но колонки берутся из сохранённой настройки, а не из markdown-строки `extra`. Типы колонок там (`CharacterName / Player / Contacts / FromField / Groups / PublicGroups`) ложатся на поля настройки один-к-одному.

Решение:
==

Реализовать страницу как **read-only Blazor-island** (стек как у управления сетками, но только на чтение — один GET-метод, без мутаций), следуя [ADR001](adr001-blazor.md) и [view-pages-guide.md](view-pages-guide.md).

Ключевые решения по размещению и доступу:
- **UI-проект: `JoinRpg.Web.CharacterGroups`** (игроцкий, тематически — «группы персонажей»), а не `ProjectMasterTools`. Туда кладём read-only клиент, ViewModel'и и компонент отображения. Управление сетками остаётся в `ProjectMasterTools`. У них **раздельные** `I*Client` и ViewModel'и.
- **Переиспользование link-компонентов**: ячейки персонажей/групп рисуются готовыми `CharacterLink` / `CharacterGroupLink` из `JoinRpg.Web.ProjectCommon` (и их `*SlimViewModel`), а не ручными `<a>`. Для этого `JoinRpg.Web.CharacterGroups` получает `ProjectReference` на `JoinRpg.Web.ProjectCommon` (цикла нет: ProjectCommon на CharacterGroups не ссылается).
- **Авторизация по `PublicMode` (динамическая, не атрибутом).** Доступ зависит от загруженной настройки: `PublicMode == true` → виден всем (в т.ч. анонимам); `false` → только мастеру. Это нельзя выразить статическим `[RequireMaster]` на странице/контроллере — проверка делается **в серверной реализации** после загрузки `config`: если `!config.PublicMode` и текущий пользователь не мастер → `NoAccessToProjectException`. Поэтому страница и WebAPI-контроллер **не** помечаются `[RequireMaster]` (иначе публичная сетка была бы недоступна игроку).
- **Структурный ViewModel** — билдер отдаёт сериализуемые (JSON через WASM-границу) данные. HTML собирает Blazor-компонент.
- **Чистый тестируемый Builder** (Domain → ViewModel) без зависимости от `ICurrentUserAccessor`. `PublicMode` гейтит доступ к **странице целиком** (проверка в сервисе), а режимы колонок (`ContactsColumn`/`GroupsColumn` = `None/PublicOnly/All`) — это осознанный выбор мастера, *что именно* показать на сетке; они кодируют намерение в самой настройке и не зависят от личности зрителя. Поэтому билдер остаётся user-independent: и мастер, и игрок на публичной сетке видят одинаковый набор колонок, заданный настройкой.

Подробности
==

### 1. ViewModel сетки (сериализуемый)
Файл: новый, `src/JoinRpg.Web.CharacterGroups/ProjectRoleGrid/ViewModels.cs`.

Вместо одного списка ячеек с дискриминатором `Kind` — **отдельные типизированные классы** для каждого типа ячейки, разложенные по **разным членам** строки. Это убирает полиморфизм/switch и делает рендер прямым. Ячейки-ссылки переиспользуют slim-VM из `ProjectCommon`.

Порядок колонок (п.3): **Персонаж → Игрок (если есть) → Группы (если есть) → Поля**.

```csharp
using JoinRpg.Web.ProjectCommon; // CharacterLinkSlimViewModel, CharacterGroupLinkSlimViewModel

public record ProjectRoleGridViewModel(
    string Name,
    string? GroupName,
    bool HasPlayerColumn,
    bool HasGroupsColumn,
    IReadOnlyList<string> FieldColumnNames,           // заголовки колонок-полей, по порядку
    IReadOnlyList<ProjectRoleGridRowViewModel> Rows);

public record ProjectRoleGridRowViewModel(
    CharacterLinkSlimViewModel Character,             // персонаж (всегда)
    PlayerCellViewModel? Player,                      // null, если колонки «Игрок» нет
    GroupsCellViewModel? Groups,                      // null, если колонки «Группы» нет
    IReadOnlyList<string> FieldValues);               // выровнено с FieldColumnNames

// Отдельные классы ячеек:
public record PlayerCellViewModel(string Name, UserContacts? Contacts);  // имя/статус + контакты (или null)
public record GroupsCellViewModel(IReadOnlyList<CharacterGroupLinkSlimViewModel> Groups);
```

Компонент рендерит члены строки напрямую в фиксированном порядке (Персонаж, Игрок, Группы, Поля) — без `Kind` и без switch. Флаги `HasPlayerColumn`/`HasGroupsColumn` и `FieldColumnNames` на уровне грида задают набор и заголовки колонок; в строках соответствующие члены `null`/пусты, если колонки нет. Всё полностью JSON-friendly.

Контакты выносим в **отдельную переиспользуемую модель** `UserContacts` с типизированными полями (новый файл `src/JoinRpg.Common.PrimitiveTypes/Users/UserContacts.cs`):

```csharp
namespace JoinRpg.Common.PrimitiveTypes;

/// <summary>Контакты пользователя для отображения</summary>
public record UserContacts(Email? Email, VkId? VkId, TelegramLink? Telegram);
```

Для Telegram заводим **новый value-тип `TelegramLink`** (новый файл `src/JoinRpg.Common.PrimitiveTypes/Users/TelegramLink.cs`) — он содержит **только** имя пользователя для ссылки `t.me/…`, без числового `Id`:

```csharp
namespace JoinRpg.Common.PrimitiveTypes;

[TypedStringValue]
public partial record TelegramLink(string Value);
```

Это отличается от существующего `TelegramId(long Id, PrefferedName? UserName)`: для отображения ссылки нужен только `@username`, а он лежит прямо в `UserExtra.Telegram` (строка). Поэтому `UserContacts` использует `TelegramLink`, а не `TelegramId` — не нужно тащить числовой `Id` из `ExternalLogins` (это снимает прежний открытый вопрос о загрузке `ExternalLogins`). `TelegramLink.FromOptional(user.Extra?.Telegram)` (генерируется `[TypedStringValue]`).

`UserContacts` использует существующие `Email`, `VkId` плюс новый `TelegramLink` (все в `JoinRpg.Common.PrimitiveTypes`). Это не `UserSocialNetworks` (тот хранит `VkId` строкой и без `Email`) — `UserContacts` именно для отображения контактов. Ссылки (`mailto:`, `vk.com/…`, `t.me/…`) строит UI-компонент из этих полей (см. п.7), а не билдер.

### 2. Контракт клиента (отдельный, read-only)
Файл: новый, `src/JoinRpg.Web.CharacterGroups/ProjectRoleGrid/IProjectRoleGridClient.cs`.

```csharp
public interface IProjectRoleGridClient
{
    Task<ProjectRoleGridViewModel> GetRoleGrid(ProjectRolesListIdentification id);
}
```

Не путать с мастерским `IProjectRolesListClient` (управление) в `ProjectMasterTools`.

### 3. Серверная реализация
Файл: новый, `src/JoinRpg.WebPortal.Managers/CharacterGroups/ProjectRoleGridViewService.cs`, реализует `IProjectRoleGridClient`. Зависимости: `IProjectRepository`, `IProjectMetadataRepository`, **`ICurrentUserAccessor`** (для проверки доступа по `PublicMode`). Регистрация в `JoinRpg.WebPortal.Managers.Registration.cs`.

Настройка сетки (`ProjectRolesList`) **уже лежит в `ProjectInfo`** (`projectInfo.ProjectRolesLists`) — **отдельный `IProjectRolesListRepository` не нужен**.

Логика `GetRoleGrid` (повторяет `GroupWrapper` из рендерера):
1. `var projectInfo = await projectMetadataRepository.GetProjectMetadata(id.ProjectId)ProjectRolesLists;`
2. `var config = projectInfo.ProjectRolesLists.SingleOrDefault(x => x.ProjectRolesListId == id) ?? throw ...;` (завести хелпер `ProjectInfo.GetRolesListById` по образцу `GetGroupById`/`GetFieldById`).
3. **Проверка доступа по `PublicMode`:** если `!config.PublicMode` и текущий пользователь не мастер — `throw new NoAccessToProjectException(projectInfo, currentUserAccessor.UserIdOrDefault?.Value);`. Проверка мастера — `projectInfo.HasMasterAccess(currentUserAccessor.UserIdentificationOrDefault)`. Публичная сетка (`PublicMode == true`) доступна и анонимам, поэтому `UserIdentificationOrDefault` может быть `null`.
4. Группа: `var groupId = config.CharacterGroupId ?? projectInfo.RootCharacterGroupId;`
5. Загрузить сами `Character` (для полей/контактов) через `projectRepository.GetCharacterByGroups(projectInfo.GetChildGroupIdsIncludingThis(groupId).ToList())`, отфильтровать `IsActive`, сгруппировать по `ParentCharacterGroupIds`.
6. **Детерминированный порядок персонажей** (см. ниже) — упорядоченный DFS по группам из снепшота, внутри каждой группы персонажи сортируются по `ChildCharactersOrdering`.
7. Имя группы — `projectInfo.GetGroupById(groupId.CharacterGroupId).Name`. Передать упорядоченный `IReadOnlyList<Character>` в билдер (4).

> Проверка доступа живёт в сервисе (а не в атрибуте), потому что зависит от данных (`config.PublicMode`), которые известны только после загрузки. `NoAccessToProjectException` перехватывается `CaptureNoAccessExceptionFilter` и превращается в корректный ответ.

**Детерминированный порядок персонажей (резолв открытого вопроса).** Порядок строк следует настройкам сортировки `ChildGroupsOrdering` (порядок дочерних групп) и `ChildCharactersOrdering` (порядок персонажей внутри группы), а не произвольному порядку выборки. Нужных данных **достаточно в снепшоте `ProjectInfo`** — персонажи в снепшот **не** грузятся:
- Порядок дочерних групп уже учтён в `CharacterGroupInfo.DirectChildGroupIds` (сортируется в `CharacterGroupDictionaryBuilder` по `ChildGroupsOrdering`).
- Порядок персонажей внутри группы — в новом поле `CharacterGroupInfo.ChildCharactersOrdering` (та же строка-ordering, что на EF-сущности; в снепшот затащена как есть).

Сервис строит порядок так: `projectInfo.GetChildGroupsIncludingThis(groupId)` даёт группы поддерева (включая саму группу) в порядке упорядоченного DFS. Сам порядок уже зашит в снепшоте — поле `CharacterGroupInfo.AllChildGroupsIncludingThis` строится в `CharacterGroupDictionaryBuilder` упорядоченным preorder-DFS (дочерние группы в порядке `ChildGroupsOrdering`, дедупликация по первому вхождению), метод лишь проецирует id → `CharacterGroupInfo`. Затем для каждой группы её прямые (уже загруженные) персонажи сортируются `characters.OrderByStoredOrder(c => c.CharacterId, group.ChildCharactersOrdering)` (`JoinRpg.Helpers`), результат конкатенируется и дедуплицируется по `CharacterId` — персонаж в нескольких группах берётся по **первому** вхождению в DFS. Это строже, чем `ExperimentalTableFunc`, где упорядочены только персонажи внутри группы, но не сами группы.

Сами `Character` (для значений полей и контактов: `ApprovedClaim.Player.Email`, `Player.Extra` с Vk/Telegram/`SocialNetworksAccess`) грузим обычным `GetCharacterByGroups(groupIds)` (`groupIds = projectInfo.GetChildGroupIdsIncludingThis(groupId)`). `LoadGroupWithTreeAsync`/`OrderingExtensions` и `ExternalLogins` больше **не** нужны (порядок — из снепшота, Telegram-ссылка — из `Extra.Telegram`).

### 4. Builder (чистая функция, тестируемый)
Файл: новый, `src/JoinRpg.WebPortal.Managers/CharacterGroups/ProjectRoleGridViewModelBuilder.cs`.

```csharp
internal static class ProjectRoleGridViewModelBuilder
{
    public static ProjectRoleGridViewModel Build(
        ProjectRolesList config,
        string? groupName,
        IReadOnlyCollection<Character> characters,
        ProjectInfo projectInfo);
}
```

Колонки в порядке (п.3): **Персонаж → Игрок → Группы → Поля**.
- **Персонаж** (`Character`) — всегда; `CharacterLinkSlimViewModel(characterId, name, isActive, ViewMode)`.
- **Игрок** (`Player`) — `HasPlayerColumn = config.ContactsColumn != None`. Имя/статус: повторить switch из `GetPlayerString` (NPC / «шаблон» / «нет игрока» / `player.GetDisplayName()`). Контакты (`UserContacts` или `null`) зависят от режима (резолв открытого вопроса про `PublicOnly`):
  - `All` → контакты показываем **безусловно** (мастерский режим: мастер видит всё).
  - `PublicOnly` → контакты показываем **только если игрок разрешил это в настройках профиля**: `user.Extra?.SocialNetworksAccess == ContactsAccessType.Public`. Иначе `Contacts = null` (только имя). Так уважаем приватность профиля.
  - Архивный проект (`projectInfo.ProjectStatus == Archived`) → контакты не показываем (`Contacts = null`) в любом режиме — как в `GetPlayerString(showContacts: false)`.
  - Сами контакты собираем в `UserContacts`: `Email` из `user.Email`; `VkId` через `VkId.FromOptional(user.Extra?.Vk)`; `Telegram` через `TelegramLink.FromOptional(user.Extra?.Telegram)`. `VkVerified` **не** проверяем (как в markdown-рендерере; резолв открытого вопроса).
- **Группы** (`Groups`) — `HasGroupsColumn = config.GroupsColumn != None`. `GroupsCellViewModel` из `character.GetIntrestingGroupsForDisplayToTop(projectInfo)` (для `PublicOnly` фильтр `g.IsPublic`) → `CharacterGroupLinkSlimViewModel`.
- **Поля** (`FieldValues`, `FieldColumnNames`) — по `config.Fields`: для каждого `projectInfo.GetFieldById(id)` заголовок = `field.Name`, значение = `character.GetFieldsDict(projectInfo)[id].DisplayString` (выровнено по порядку с `FieldColumnNames`).

> Короткий switch статуса игрока дублирует приватный `GetPlayerString` в `JoinrpgMarkdownLinkRenderer`. Рендерер не трогаем — дублируем небольшой кусок в билдере с TODO о возможной будущей унификации. HTML-хелпер `GetPlayerContacts` через WASM-границу не переиспользуем — отдаём типизированную `UserContacts`, а ссылки строит компонент.

### 5. WebAPI-контроллер
Файл: новый, `src/JoinRpg.Portal/Controllers/WebApi/ProjectRoleGridController.cs` (отдельный от мастерского `ProjectRolesListController`).

**Без `[RequireMaster]`** — доступ к публичной сетке должен быть и у игрока/анонима; реальную проверку (`PublicMode` + мастер) делает сервис (см. п.3). Контроллер просто проксирует:

```csharp
[Route("/webapi/project-role-grid/[action]")]
public class ProjectRoleGridController(IProjectRoleGridClient client) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProjectRoleGridViewModel>> Get(
        [FromQuery] ProjectIdentification projectId, [FromQuery] int projectRolesListId)
    {
        var id = new ProjectRolesListIdentification(projectId, projectRolesListId);
        return Ok(await client.GetRoleGrid(id));
    }
}
```

### 6. HTTP-клиент (WASM)
Файл: новый, `src/JoinRpg.Blazor.Client/ApiClients/ProjectRoleGridClientImpl.cs` (по образцу `GetList`, только `GetFromJsonAsync`, без CSRF). Регистрация в `HttpClientRegistration.cs`:
`.AddHttpClient<IProjectRoleGridClient, ProjectRoleGridClientImpl>()`.

### 7. Blazor-компоненты
Файл: новый, `src/JoinRpg.Web.CharacterGroups/ProjectRoleGrid/ProjectRoleGrid.razor`.
- `@inject IProjectRoleGridClient Client`, параметр `[Parameter] ProjectRolesListIdentification RolesListId`.
- `OnInitializedAsync` → `_model = await Client.GetRoleGrid(RolesListId);`, до загрузки — `<JoinLoadingMessage/>`.
- Рендер: `<h4>@_model.GroupName</h4>` + `<table class="table">`. Заголовки в порядке: «Персонаж», «Игрок» (если `HasPlayerColumn`), «Группы» (если `HasGroupsColumn`), затем `FieldColumnNames`. Строки рисуют члены напрямую (без `Kind`): `<td><CharacterLink></td>`; если `row.Player != null` — `<td>@Player.Name <UserContactsView Contacts="Player.Contacts"/></td>`; если `row.Groups != null` — `<td>` со списком `<CharacterGroupLink>` через `, `; затем по `row.FieldValues` — `<td>@value</td>`.

Файл: новый переиспользуемый компонент `src/JoinRpg.Web.ProjectCommon/UserContactsView.razor` — принимает `[Parameter] UserContacts? Contacts` и рисует ссылки `mailto:{Email.Value}`, `https://vk.com/{VkId.Value}`, `https://t.me/{Telegram.Value.TrimStart('@')}` (только заполненные поля). Кладём в `ProjectCommon`, т.к. это общий контактный виджет, полезный и на других страницах.

### 8. Razor-страница (замена заглушки)
Файл: `src/JoinRpg.Portal/Pages/GamePages/ProjectRoleListView.cshtml` — заменить `TODO`:

```razor
@using JoinRpg.Web.CharacterGroups.ProjectRoleGrid;
...
@(await Html.RenderComponentAsync<ProjectRoleGrid>(RenderMode.WebAssembly, new { Model.RolesListId }))
```

Code-behind `ProjectRoleListView.cshtml.cs`: **снять атрибут `[RequireMaster]`** (сейчас он там есть) — иначе публичная сетка недоступна игроку/анониму. `RolesListId` оставить. Доступ enforce'ится в сервисе по `PublicMode` (п.3); страница лишь рендерит компонент, который дёргает WebAPI. Если сетка приватная и доступа нет — `NoAccessToProjectException` из сервиса даст корректную ошибку.

### 9. Навигация
В мастерской строке `src/JoinRpg.Web.ProjectMasterTools/ProjectRolesLists/ProjectRolesListRow.razor` добавить ссылку «Открыть сетку» на `/{projectId}/roleslist/{id}` (роут уже существует).

### 10. Зависимости проектов
- `JoinRpg.Web.CharacterGroups` → добавить `ProjectReference` на `JoinRpg.Web.ProjectCommon` (для `CharacterLink`/`CharacterGroupLink`, `UserContactsView` и slim-VM). Цикла нет.
- `JoinRpg.WebPortal.Managers` → добавить `ProjectReference` на `JoinRpg.Web.CharacterGroups` (чтобы сервис п.3 реализовывал `IProjectRoleGridClient` и возвращал `ProjectRoleGridViewModel`). По образцу существующей ссылки `Managers → JoinRpg.Web.ProjectMasterTools`. `Managers` уже ссылается на `JoinRpg.Web.ProjectCommon`, так что slim-VM доступны билдеру и напрямую.
- `UserContacts` и `TelegramLink` живут в `JoinRpg.Common.PrimitiveTypes/Users/` — доступны и в ViewModel'ях (`CharacterGroups` → `DomainTypes` → `Common.PrimitiveTypes`), и в билдере (`Managers`), и в `ProjectCommon`.

### 11. Тест билдера
Файл: новый, `src/JoinRpg.WebPortal.Managers.Test/CharacterGroups/ProjectRoleGridViewModelBuilderTests.cs`. Проверить: состав/порядок колонок при разных `ContactsColumn`/`GroupsColumn`/`Fields`; содержимое ячеек (ссылка персонажа, статус «нет игрока»/«NPC», контакты, поля, группы). Отдельно покрыть резолвы:
- **Порядок строк** — что персонажи идут в порядке DFS (`ChildGroupsOrdering` → `ChildCharactersOrdering`) и дедуплицируются (первое вхождение).
- **`PublicOnly` + приватность** — игрок с `SocialNetworksAccess == Public` → контакты есть; `OnlyForMasters` → `Contacts == null` (только имя). В режиме `All` контакты есть независимо от настройки.
- **Архивный проект** — `Contacts == null` в любом режиме.
- **`TelegramLink`** — собирается из `Extra.Telegram` (без числового `Id`).

Билдер user-independent — `PublicMode` его не затрагивает (это проверка доступа в сервисе).

Проверку доступа по `PublicMode` тестируем на уровне сервиса (`ProjectRoleGridViewService`) с `FakeCurrentUserAccessor`: приватная сетка + не-мастер → `NoAccessToProjectException`; приватная + мастер → ок; публичная + аноним → ок.

Решённые вопросы
==

- **Порядок персонажей** — детерминированный упорядоченный DFS по снепшоту `ProjectInfo` (`ProjectInfo.GetChildGroupsIncludingThis`, порядок зашит в `CharacterGroupInfo.AllChildGroupsIncludingThis`) + сортировка персонажей внутри каждой группы по `ChildCharactersOrdering`. Порядок дочерних групп уже хранится в `CharacterGroupInfo.DirectChildGroupIds`; добавлено поле `CharacterGroupInfo.ChildCharactersOrdering` (строка-ordering), персонажи в снепшот **не** грузятся. Отдельная загрузка дерева групп больше не нужна. См. п.3.
- **Семантика `ContactsColumn.PublicOnly`** — показываем контакты игрока, только если он разрешил это в профиле (`UserExtra.SocialNetworksAccess == ContactsAccessType.Public`); иначе только имя. `All` — безусловно. См. п.4.
- **`TelegramId` для отображения** — заменён на новый `[TypedStringValue] TelegramLink` (только `@username`), берётся из `UserExtra.Telegram`; `ExternalLogins`/числовой `Id` больше не нужны. См. п.1.
- **JSON-сериализация `UserContacts`** и **`VkVerified`** — оставляем как есть: round-trip `Email`/`VkId`/`TelegramLink` (`[TypedStringValue]`) корректен дефолтными конвертерами; `VkVerified` не проверяем (как в markdown-рендерере).
- **Публичная сетка + `ContactsColumn == All`** — недопустимая комбинация, запрещена на уровне настройки. `ProjectRolesList.Validate` (DataModel) и `AddProjectRolesListViewModel.Validate` (ProjectMasterTools) при сохранении дают ошибку «В публичной сетке ролей нельзя показывать все контакты» для `PublicMode && ContactsColumn == All` (commit #4316). Поэтому на публичной сетке аноним никогда не увидит «все контакты»: режим `All` доступен только на приватной (мастерской) сетке. Билдеру и сервису отображения дополнительных проверок не нужно — инвариант гарантирован конфигурацией; но билдер обязан корректно отработать `PublicOnly` (контакты только если игрок их открыл), что и так покрыто (п.4).

Последствия
==

- **Правильное разделение аудитории** — мастерское управление (`ProjectMasterTools`) и игроцкое отображение (`CharacterGroups`) разведены.
- **Доступ по `PublicMode`** — публичные сетки видны игрокам/анонимам, приватные — только мастерам; проверка динамическая (в сервисе), а не статическим атрибутом.
- **Переиспользование** — ячейки используют готовые `CharacterLink`/`CharacterGroupLink`; реализация становится рабочим примером для [view-pages-guide.md](view-pages-guide.md).
- **Типизированная модель контактов** — `UserContacts` (`Email`/`VkId`/`TelegramLink`) и виджет `UserContactsView` переиспользуемы за пределами этой фичи. Новый value-тип `TelegramLink` (ссылка без числового `Id`) — тоже общеполезен.
- **Дублирование** — небольшой switch статуса игрока временно дублируется с `JoinrpgMarkdownLinkRenderer`.

Статус
==

Принят, готов к реализации. Предпосылки уже в `master`:
- `CharacterGroupInfo.ChildCharactersOrdering` (порядок персонажей внутри группы) — снепшот несёт строку-ordering, персонажи не грузятся (#4331).
- `CharacterGroupInfo.AllChildGroupsIncludingThis` / `ProjectInfo.GetChildGroupsIncludingThis` — упорядоченный preorder-DFS по дереву групп.
- Запрет «публичная сетка + все контакты» на уровне настройки: `ProjectRolesList.Validate` (DataModel) и `AddProjectRolesListViewModel.Validate` (ProjectMasterTools) (#4316).

Реализовано (пп. 1–11): `UserContacts` (PrimitiveTypes), ViewModel'и и `IProjectRoleGridClient` (CharacterGroups), `ProjectRoleGridViewService` + `ProjectRoleGridViewModelBuilder` + `ProjectInfo.GetRolesListById` (Managers), WebAPI `ProjectRoleGridController` + HTTP-клиент, компоненты `ProjectRoleGrid.razor`/`UserContactsView.razor`, страница без `[RequireMaster]`, навигация и тесты билдера.

Пересмотр контактов (#4349): вместо собственного `[TypedStringValue] TelegramLink` (только `@username`) переиспользуем канонический `TelegramId` (числовой `Id` из привязанного telegram-логина + `@username`) и новые типизированные Blazor-компоненты `EmailLink`/`VkLink`/`TelegramLink`/`LiveJournalLink` (`JoinRpg.WebComponents`). Свой `TelegramLink`-тип удалён: он конфликтовал по имени с компонентом `TelegramLink.razor` и дублировал `TelegramId.UserName`. `UserContacts` теперь несёт `Email`/`VkId`/`TelegramId`/`LiveJournalId`; `UserContactsView` композирует готовые компоненты. Нюанс: telegram теперь показывается только для игроков с привязанным telegram-логином (как в `UserExtensions.GetUserInfo`), а не для произвольно введённого `@username` (как было в markdown-рендерере).

Режим дерева (15.07.2026): настройка `ShowCharacterGroups` (bool) заменена на enum `RolesGridGroupsViewMode { None, Sections, Tree }` (колонка `GroupsViewMode`, миграция переименованием bit→int: false=0=None, true=1=Sections). В режиме `Tree` билдер строит строки собственным preorder-DFS по `CharacterGroupInfo.DirectChildGroupIds` (глубина, путь для tooltip, пустые группы включаются, повторные вхождения группы/персонажа помечаются `FirstCopy == false` — «см. выше», непубличные ветки отсекаются для не-мастера, спецгруппы последними с `BoundExpression`), а компонент рендерит иерархию с отступами по образцу классической сетки (`GameGroups/Index`); поля показываются строками под именем персонажа. Общая ячейка игрока вынесена в `ProjectRoleGridPlayerCell.razor` и дополнена счётчиком активных заявок (`ActiveClaimsCount`).

---
*Создано: 07.06.2026*
*Обновлено: 18.06.2026 — синхронизировано с master: переименование `GetOrderedSubtreeGroups` → `GetChildGroupsIncludingThis`, добавлена зависимость `Managers → CharacterGroups`, отмечены завершённые предпосылки.*
*Обновлено: 19.06.2026 — контакты переведены на канонический `TelegramId` и типизированные компоненты `EmailLink`/`VkLink`/`TelegramLink`/`LiveJournalLink` (#4349); собственный `TelegramLink`-value-тип удалён.*
