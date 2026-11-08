# Дизайн: объединение StringInput и JoinTextInput + отвязка строкового поля от Bootstrap 3

Дата: 2026-11-08
Закрывает: #6134, #6137 (часть эпика #6147 «WebComponents: убрать зависимость от Bootstrap 3»)
Образец: `2026-11-08-join-input-number-design.md` (PR #6166) — та же механика объединения.

## Зачем

В `JoinRpg.Common.WebComponents` два строковых поля дублируют друг друга:

- `JoinTextInput.razor` — основной компонент: `Immediate`, `Trim`, `Placeholder`,
  `Variants` (datalist), `AdditionalAttributes`, опциональный EditContext
  (`ValueExpression`/`FieldIdentifier`/`NotifyFieldChanged`), захват id у
  `FormRowLabelTarget`. Ставит Bootstrap-класс `form-control`.
- `StringInput.razor` — ручной razor-компонент для MVC-форм (без EditContext):
  `Name`, `Required`/`RequiredMessage` (атрибуты `data-val-*` и span для
  jquery.validate.unobtrusive), whitespace-only ввод → `null`. Используется в
  `CharacterTypeSelector.razor:39` и `PlotElementEditPanel.razor:40`, тоже ставит
  `form-control` и рядом Bootstrap-span `field-validation-valid text-danger`.

Два `<input type="text">`, два захвата `FormRowLabelTarget`, оба завязаны на Bootstrap 3.

## Решение (согласовано с @leotsarev)

Один razor-компонент **`JoinTextInput`** (имя с префиксом Join — публичный API
пакета, как в прошлый раз). В него переносятся `Name` и `Required`/`RequiredMessage`
из `StringInput` (решение по Required: переносим — клиентская проверка обязательности
в Character/Create+Edit сохраняется, span оформляется scoped CSS вместо `text-danger`).
`StringInput.razor` удаляется.

## Цели

- Один компонент строкового ввода со всеми возможностями обоих предков.
- Параметры: существующие (`Value`, `ValueChanged`, `ValueExpression`, `Placeholder`,
  `Immediate`, `Trim`, `Variants`, `AdditionalAttributes`) плюс новые `Name`,
  `Required`, `RequiredMessage` (дефолт — русская строка «Это поле обязательно
  к заполнению», как у StringInput).
- Scoped CSS вместо `form-control`; span валидации — без Bootstrap-классов.
- Оба существующих использования переезжают без поведенческих регрессий.
- Демо в WebComponentBook, покрыто в `/no-bootstrap`.

## Не-цели

- `JoinTextArea`, `JoinMarkdownTextArea`, date/time picker'ы — свои issue эпика.
- Перевод `CharacterTypeSelector` с MVC-постинга на EditForm — отдельная задача;
  здесь `Name`-постинг сохраняется как есть.

## Детали реализации

### Компонент

Файлы: `JoinTextInput.razor` + новый `JoinTextInput.razor.css` (scoped).
`StringInput.razor` удаляется.

Разметка input — как сейчас, без `class="form-control"`, плюс при `Required`:
`data-val="true"`, `data-val-required="@RequiredMessage"` (только при Required —
как у StringInput: без Required атрибуты не рендерятся). После input при
`Required` — `<span class="field-validation-valid" data-valmsg-for="@inputId"
data-valmsg-replace="true"></span>` (jquery.validate.unobtrusive заменяет класс
на `field-validation-error` и пишет сообщение; scoped CSS красит оба класса,
заменяя Bootstrap `text-danger`).

`name` атрибут: `Name` (новый параметр) → fallback `FieldIdentifier.FieldName`
(при заданном `ValueExpression`) → не рендерится (сейчас рендерится пустой —
визуально эквивалентно, разметка чище).

`id`/`inputId`: `LabelTarget?.Claim(this, ExplicitId(AdditionalAttributes))` как
сейчас; при null (вне FormRow, без явного id) — fallback на `Name`, далее на
стабильный сгенерированный id (`join-text-input-{Guid}`). Нужен непустой id для
подписи и как крайний fallback. `data-valmsg-for` span'а обязательства — по
`InputName` (т.е. `name` поля), а не по id: jquery.validate.unobtrusive ищет span
по `input.name`, как `Html.ValidationMessageFor`; при пустом `InputName` — fallback
на `inputId` (как раньше у StringInput, где id совпадал с name).

`Value` остаётся `string` (не-nullable, default `""`), как у текущего
JoinTextInput: `@bind-Value` к `string?`-полям уже работает (компонент внутри
терпит null Value), whitespace-only после Trim уходит наружу как `""` — серверная
проверка `CharacterTypeInfo` (`FieldRequiredException` на null/""/"   ") ведёт себя
идентично прежнему `null`.

### Scoped CSS

Минимум Bootstrap 3 `.form-control`, как в `JoinInputNumber.razor.css`
(те же значения: box-sizing, width/padding, border/radius, `:focus` с `#66afe9`),
плюс классы валидации: `.field-validation-valid { display: none; }`,
`.field-validation-error { color: #a94442; }` (цвет Bootstrap `text-danger`).
Визуал сверяется с сайтом (Portal под Bootstrap 3) и `/no-bootstrap`.

### Переезд использований

- `src/JoinRpg.Web.CharacterGroups/CharacterTypeSelector.razor:39` —
  `<StringInput @bind-Value="@slotName" name="@(Name + ".SlotName")" Required="true" />`
  → `<JoinTextInput @bind-Value="@slotName" Name="@(Name + ".SlotName")" Required="true" />`.
- `src/JoinRpg.Web.Plots/Elements/PlotElementEditPanel.razor:40` —
  `<StringInput Name="Content" Value="@Model.Content" />`
  → `<JoinTextInput Name="Content" Value="@Model.Content" ValueChanged="@(value => Model.Content = value)" />`.
  Two-way биндинг (а не прежний one-way без `ValueChanged`): иначе перерисовка панели
  (например, чекбокс «Мастерский текст») сбрасывала бы набранный текст к `Model.Content` —
  старый StringInput рендерил `value` напрямую и переживал перерисовки.

### Тесты

`JoinTextInputTest.cs` — дополнить:
- `Name` рендерится атрибутом `name`; без `Name`/`ValueExpression` атрибут не рендерится;
  при `ValueExpression` без `Name` name берётся из выражения;
- `Required="true"` → `data-val="true"`, `data-val-required` с дефолтным русским
  сообщением, span `data-valmsg-for` ссылается на id поля; `RequiredMessage`
  переопределяет текст; без `Required` — ни атрибутов, ни span;
- явный `id` через AdditionalAttributes выигрывает у генерации.

`FormRowTest.cs` — кейсы `StringInput` заменить на `JoinTextInput` (механически,
по аналогии с прошлым PR: параметр `Value`).

### Пакетные формальности (пакет IsPackable)

- `CHANGELOG.md` — «Не выпущено»: удалён `StringInput` (breaking), `JoinTextInput`
  больше не требует Bootstrap-классов и получил `Name`/`Required`/`RequiredMessage`.
- `PublicAPI.Shipped/Unshipped.txt` — перегенерировать `pwsh scripts/Update-PublicApi.ps1`.
- `CompatibilitySuppressions.xml` — CP0001 (удалён тип StringInput); подавление
  подтверждено прогоном pack с `PackageValidationBaselineVersion=2026.9.651`
  (CP0008 не потребовался — базовый тип и сигнатуры JoinTextInput не менялись).
- `README.md`, раздел «Стили» — `JoinTextInput` перенести из Bootstrap-зависимых
  в список работающих без Bootstrap; `StringInput` убрать.

### Демо

`Shared/JoinTextInputDemo.razor` в WebComponentBook по образцу `JoinInputNumberDemo`:
обычное поле, обязательное (с Required), с Variants; регистрация в `AllDemos.razor`
— появится на `/no-bootstrap`.

## Проверка

- `dotnet build`, `dotnet format --verify-no-changes --severity warn`.
- `dotnet test src/Common/WebComponents/JoinRpg.Common.WebComponents.Test`.
- Затронутые проекты потребителей собираются (Web.CharacterGroups, Web.Plots).
- Визуально: WebComponentBook (`/` и `/no-bootstrap`), формы редактирования
  персонажа (Character/Edit, Character/Create) и вводных (Plots).
