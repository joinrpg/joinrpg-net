# Руководство по созданию страниц просмотра

Страница просмотра — это read-only страница, которая показывает данные проекта (таблицу, карточку, список) без редактирования. Реализуется тем же Blazor-«островом», что и страницы редактирования, но содержит только чтение.

Общие шаги (стартовые решения, слои, паттерн Builder, ViewModel, Razor-страница, навигация, авторизация) описаны в [ui-pages-common.md](ui-pages-common.md). Здесь — только **отличия от страниц редактирования** ([editing-pages-guide.md](editing-pages-guide.md)).

Рабочий пример — страница просмотра сетки ролей `ProjectRoleGrid` ([ADR008](adr008-roles-list-view-page.md)).

> **Аудитория и проект.** Страницы просмотра часто видны игроку — тогда их код кладём **не в `ProjectMasterTools`**, а в профильный игроцкий `JoinRpg.Web.*` проект (см «Выбор UI-проекта» в [ui-pages-common.md](ui-pages-common.md)). Ссылки на персонажей/группы рисуем готовыми компонентами `CharacterLink` / `CharacterGroupLink` из `JoinRpg.Web.ProjectCommon` (добавив на него `ProjectReference`), а не ручным HTML. Если страница и в v1 закрыта `[RequireMaster]`, но в перспективе игроцкая — всё равно размещаем её в игроцком проекте, чтобы ослабить доступ позже без переезда кода.

## Чем страница просмотра отличается от страницы редактирования

| Аспект | Редактирование | Просмотр |
|---|---|---|
| Сервис/репозиторий | Чтение + мутации (`Create`/`Update`/`Remove`) | Только чтение (`GetByIdAsync` / `GetForProjectAsync`) |
| WebAPI | GET + POST (с CSRF) | Только **GET** |
| HTTP-клиент | GET + `PostAsJsonAsync` с CSRF-токеном | Только `GetFromJsonAsync` |
| Blazor-компоненты | `*Grid` + `*Form` + `*Row` | Один компонент отображения |
| ViewModel | Включает модели формы/ввода | Структурный display-ViewModel |
| Оптимистичные обновления/откаты | Да | Не нужны |

## 1. Репозиторий / сервис — только чтение

Достаточно метода загрузки по id (или для проекта). Мутирующий сервис (`I*Service` с `Create/Update/Remove`) не нужен.

```csharp
public interface IFeatureRepository
{
    Task<FeatureDomain?> GetByIdAsync(FeatureIdentification id);
}
```

## 2. Структурный display-ViewModel

Билдер отдаёт **готовые к отрисовке структурные данные**, без HTML-строк и без логики рендеринга: ссылки как `Text` + `Href`, режимы видимости — как enum/флаги. HTML и `<a href>` собирает Blazor-компонент.

Если в ячейках разное представление (текст / список ссылок / составной блок) — используйте поле-дискриминатор вместо полиморфизма, чтобы модель оставалась JSON-сериализуемой:

```csharp
public enum CellKind { Text, Links, Player }
public record LinkViewModel(string Text, string? Href);
public record CellViewModel(CellKind Kind, string Text, IReadOnlyList<LinkViewModel> Links);
```

> Существующие HTML-хелперы рендеринга (например, `JoinrpgMarkdownLinkRenderer.GetPlayerContacts`) возвращают готовый HTML и **не переиспользуются через WASM-границу**. Вместо них собирайте структурные данные из доменных примитивов (`User.GetDisplayName()`, `UserSocialLink.GetEmailUri/GetVKUri/GetTelegramUri` и т.п.).

## 3. Builder без зависимости от пользователя

Если страница под `[RequireMaster]` и содержимое не зависит от личности пользователя (нет кнопок редактирования), Builder делается без `ICurrentUserAccessor` — это упрощает и сам код, и тесты. Состав отображения (например, какие колонки показывать) определяется настройкой/доменной моделью, а не правами текущего пользователя.

## 4. WebAPI — только GET

```csharp
[Route("/webapi/<feature>/[action]")]
[RequireMaster]
public class FeatureController(IFeatureClient client) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<FeatureViewModel>> Get(
        [FromQuery] ProjectIdentification projectId, [FromQuery] int featureId)
    {
        var id = new FeatureIdentification(projectId, featureId);
        return Ok(await client.Get(id));
    }
}
```

## 5. HTTP-клиент — только чтение

CSRF-токен и `PostAsJsonAsync` не нужны:

```csharp
public async Task<FeatureViewModel> Get(FeatureIdentification id)
    => await httpClient.GetFromJsonAsync<FeatureViewModel>(
           $"webapi/<feature>/get?projectId={id.ProjectId.Value}&featureId={id.FeatureId}")
       ?? throw new Exception("Couldn't get result from server");
```

## 6. Один Blazor-компонент отображения

`*Form`/`*Row` для редактирования не нужны. Компонент грузит модель и рисует таблицу/карточку. Для табличного отображения концепция колонок — как в `JoinrpgMarkdownLinkRenderer.ExperimentalTableFunc`: заголовки + строки, ячейка отрисовывается по своему `Kind`.

```razor
@inject IFeatureClient Client
@if (_model is null) { <JoinLoadingMessage /> return; }

<table class="table">
  <tr>@foreach (var h in _model.ColumnHeaders) { <th>@h</th> }</tr>
  @foreach (var row in _model.Rows) { <tr>@* ... *@</tr> }
</table>

@code {
    private FeatureViewModel? _model;
    [Parameter] public FeatureIdentification Id { get; set; }
    protected override async Task OnInitializedAsync() => _model = await Client.Get(Id);
}
```

## 7. Razor-страница с id сущности в роуте

В отличие от страницы редактирования (обычно `/{projectId}/tools/<feature>`), страница просмотра конкретной сущности берёт её id из роута и прокидывает в компонент:

```razor
@page "/{projectId:int}/<feature>/{id:int}"
@(await Html.RenderComponentAsync<FeatureComponent>(RenderMode.WebAssembly, new { Model.FeatureId }))
```

## 8. Тест Builder

Как и для редактирования (см [ui-pages-common.md](ui-pages-common.md)), Builder покрывается юнит-тестами в `JoinRpg.WebPortal.Managers.Test`. Для read-only страниц проверяйте состав/порядок колонок и содержимое ячеек на разных входных данных.

## Примечания

- Следовать стилю кода проекта (русский язык UI, `TODO[Localize]` если нужно).
- Проверить работу в режиме Blazor WebAssembly.
