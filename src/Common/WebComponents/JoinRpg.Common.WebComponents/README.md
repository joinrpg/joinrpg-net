# JoinRpg.Common.WebComponents

Blazor-компоненты joinrpg.ru: кнопки, поля ввода, диалоги, панели, таблицы, подсказки, иконки,
ссылки на пользователей. Все компоненты с примерами — в каталоге
[components.joinrpg.ru](https://components.joinrpg.ru).

```
dotnet add package JoinRpg.Common.WebComponents
```

Нужен net10.0, `JoinRpg.Common.PrimitiveTypes` придёт зависимостью. Сайт — только в корне
домена: иконки и скрипты диалогов грузятся по `/_content/...`.

## Подключение

1. Пространство имён: в Blazor — `@using JoinRpg.Common.WebComponents` в `_Imports.razor`; в
   Razor Pages — в `_ViewImports.cshtml`, а компонент рендерится через
   `<component type="typeof(JoinIcon)" render-mode="Static" ... />`.

2. Иконки — в layout:

   ```html
   <link rel="stylesheet" href="_content/JoinRpg.Common.WebComponents/css/join-icons.css" />
   ```

   `css/roboto.css` — для `PrintableBadge` (бейджи печатаются Roboto) или если сайту нужен этот
   шрифт.

3. Скрипт (подсказки `Tooltip`, меню `JoinDropdownButton`, `TypedSelector`, раскрытие
   `JoinCollapsePanel`). Blazor загружает его сам при старте интерактивного рантайма; на
   странице из одних статических компонентов добавьте тег. Адрес — тот же, что у Blazor: с
   MapStaticAssets `~/` в `.cshtml` и `@Assets[...]` в `App.razor`, иначе на странице с островом
   модуль выполнится дважды.

   ```html
   <script src="~/_content/JoinRpg.Common.WebComponents/JoinRpg.Common.WebComponents.lib.module.js" type="module"></script>
   ```

4. `JoinAdminAuthorizeView` — обёртка над `AuthorizeView`: нужны `AddCascadingAuthenticationState()`,
   а в WebAssembly ещё `AddAuthorizationCore()` и `AuthenticationStateProvider`.

## Стили

Bootstrap 3 пакет не приносит. На его CSS рассчитаны `JoinPanel`, `JoinCollapsePanel`,
`JoinPanelWithList`, `JoinAlert`, таблицы, `FormRow` и поля ввода (`panel`, `list-group`, `alert`,
`table`, `form-group`, `col-md-*`, `form-control`, `help-block`, `control-label`). `JoinDotsMenu`
и `TypedDropdownButton` раскрываются только с JS Bootstrap 3 (`data-toggle`). Без Bootstrap
обходятся `JoinButton`, `JoinBadge`, `Tooltip`, `TypedSelector`, `JoinDropdownButton`,
`JoinDialog`.

## Что зарегистрировать у себя

- `IUriLocator<UserLinkViewModel>` (`IUriLocator<T>` — из PrimitiveTypes) — ссылка на профиль пользователя.
  Обязателен для `UserLink` и `JoinUserLinkEditor`: они получают его через `@inject`.
- `IUserLinkResolveClient` — превращает введённую строку (ссылку на профиль, id, vk, telegram,
  email) в пользователя для `JoinUserLinkEditor`; не нашли — киньте исключение с текстом для
  пользователя. Необязателен: без него редактор работает как обычное текстовое поле.
- `IMoveClient` — сохраняет новый порядок элементов. Обязателен, если на странице есть
  `JoinMoveControl`: компонент получает его через `@inject`.
- `AddJoinRpgWebComponents()` регистрирует `SelectorListCache` — нужен своим обёрткам над
  `TypedSelector`, которые его берут: без кэша N одинаковых селекторов делают N запросов.
  Компонентам пакета он не нужен.
