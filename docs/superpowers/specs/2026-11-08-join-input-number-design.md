# Дизайн: объединение NumberInput и JoinInputNumber + отвязка числового поля от Bootstrap 3

Дата: 2026-11-08
Закрывает: #6136, #6138 (часть эпика #6147 «WebComponents: убрать зависимость от Bootstrap 3»)

## Зачем

В `JoinRpg.Common.WebComponents` два числовых поля дублируют друг друга:

- `JoinInputNumber.cs` — наследник `InputNumber<TValue>`, живёт только внутри `EditForm`
  (4 использования: `ExpenseEditForm`, `RoomTypeEditForm`, `BadgeTemplateEditForm`,
  `FinanceFeeSettingsPanel`). Добавляет Bootstrap-класс `form-control` и цепляет id у
  `FormRowLabelTarget`.
- `NumberInput.razor` — ручной razor-компонент только для `int?`, работает **без**
  EditContext (MVC-форма в `CharacterTypeSelector.razor` постится по `name`), устойчив к
  мусорному вводу (дробь/экспонента → `null`). Используется в `CharacterTypeSelector` и
  `TimeSlotMassAddButton`.

Две разметки `<input type="number">`, два парсера, два захвата `FormRowLabelTarget`, оба
ставят `form-control` (Bootstrap 3) — без его CSS поле неоформлено, а класс удалён из
Bootstrap 4/5, что блокирует внешних потребителей пакета.

## Решение (согласовано с @leotsarev)

Один универсальный razor-компонент **`JoinInputNumber<TValue>`** по образцу
`JoinTextInput.razor`: собственная разметка, собственный scoped CSS, EditContext —
опционален. `NumberInput` и `JoinInputNumber.cs` удаляются.

## Цели

- Один компонент числового ввода: generic `TValue` (int, int?, long?, decimal и nullable-варианты).
- Работа в двух режимах:
  - **Без EditContext** (как сейчас `NumberInput`): standalone, `@bind-Value`, мусор → `null` молча.
  - **С EditForm** (как сейчас `JoinInputNumber`): `ValueExpression`/`FieldIdentifier`,
    `NotifyFieldChanged`, ошибка парсинга попадает в валидацию формы, поддержка `ParsingErrorMessage`.
- Параметры: `Value`, `ValueChanged`, `ValueExpression`, `Name`, `Min`, `Max`,
  `ParsingErrorMessage` (по умолчанию — русская строка «Укажите число»; отступление от
английского ресурса `InputNumber<TValue>`, видимое изменение — отметить в CHANGELOG). `AdditionalAttributes` (CaptureUnmatchedValues), explicit `id`
  через `FormRowLabelTarget.ExplicitId` (как у `JoinTextInput`/`FormRowLabelTarget`).
- Scoped CSS вместо `form-control` — компонент оформлен без Bootstrap.
- Все 6 существующих использований переезжают на новый компонент без визуальных регрессий.
- Демо в WebComponentBook, покрыто в `/no-bootstrap` (критерий готовности эпика #6147).

## Не-цели

- Отвязка от Bootstrap `FormRow`, `FormRowFor`, таблиц и прочих компонентов — отдельные
  issue эпика (#6143, #6144 и т.д.). Строки форм (`form-group`, `col-md-*`, `help-block`)
  остаются Bootstrap-зависимыми; в `/no-bootstrap` строки выглядят неоформленными — это
  ожидаемо и лечится своими issue.
- `StringInput`, `JoinTextInput` и другие поля ввода — свои issue (#6134, #6137).

## Детали реализации

### Компонент

Файлы: `JoinInputNumber.razor` + `JoinInputNumber.razor.css` (scoped). `JoinInputNumber.cs`
и `NumberInput.razor` удаляются.

Разметка: `<input type="number" id name value min max @onchange ...>` по образцу
текущего `NumberInput.razor`, плюс интеграция с `EditContext` по образцу
`JoinTextInput.razor:69-95` (`FieldIdentifier`, `NotifyFieldChanged` при наличии
`ValueExpression`).

Парсинг: инвариантный `TryParse`, стили — как у `InputNumber<TValue>` на каждый тип
(`NumberStyles.Number` для decimal/дробных — дроби принимаются; `Integer` для целых —
«1.5» → ошибка/null). Поведение:
- `TValue` nullable или EditContext отсутствует → мусор даёт `null` (текущее поведение `NumberInput`).
  Для не-nullable `TValue` без EditContext (`EventCallback<int>` не принимает `null`) —
  `ValueChanged` не вызывается вовсе, модель не трогаем.
- `TValue` не-nullable и EditContext есть → зеркалим `InputNumber<TValue>`: `ValueChanged`
  не вызывается, ошибка парсинга уходит в `EditContext` через `ValidationMessageStore` на
  `FieldIdentifier`, сырое значение остаётся в поле; пустой ввод у не-nullable поля —
  ошибка парсинга.

`Min`/`Max` — параметры типа `TValue?` (C# 11 unconstrained nullable annotation),
рендерятся атрибутами `min`/`max` при не-null значении. Явные `min`/`max` через
`AdditionalAttributes` тоже должны работать (не ломать существующие вызовы вида
`min="@BadgeLayoutRules.MinSizeMm"`).

`Name` — отдельный параметр (нужен MVC-постингу в `CharacterTypeSelector`);
при null не рендерится. Как у текущего `NumberInput`.

Захват id подписи: `LabelTarget?.Claim(this, FormRowLabelTarget.ExplicitId(AdditionalAttributes))`
— как у `JoinTextInput.razor:79`.

### Scoped CSS

Переносим в scoped CSS минимум Bootstrap 3 `.form-control`, нужный числовому полю:
`width: 100%`, `padding`, `border`, `border-radius`, состояние `:focus`
(цвет рамки/тень — по образцу Bootstrap 3 `#66afe9`). Заодно `input[type=number]` —
убрать стрелки спиннера не нужно (Bootstrap 3 их не трогал), оставляем как есть.
Визуал сверяется с сайтом (Portal под Bootstrap 3) и с демо `/no-bootstrap`.

### Переезд использований

- `src/JoinRpg.Web.Finances/Expenses/ExpenseEditForm.razor:22-31` — замена имени тега,
  `@bind-Value`/`ParsingErrorMessage` без изменений.
- `src/JoinRpg.Web.Accommodation/RoomTypeEditForm.razor:96-102` — то же.
- `src/JoinRpg.Web.ProjectMasterTools/Badges/BadgeTemplateEditForm.razor:29-77` — то же,
  явные `min`/`max` продолжают работать.
- `src/JoinRpg.Web.ProjectMasterTools/Finance/FinanceFeeSettingsPanel.razor:90-95` — то же.
- `src/JoinRpg.Web.CharacterGroups/CharacterTypeSelector.razor:33` —
  `<NumberInput @bind-Value="@characterSlotLimit" name="@(Name + ".SlotLimit")" Min="0"/>`
  → `<JoinInputNumber @bind-Value="characterSlotLimit" Name="@(Name + ".SlotLimit")" Min="0" />`.
- `src/JoinRpg.Web.ProjectMasterTools/Fields/TimeSlotMassAddButton.razor:35,38` —
  `Name="@nameof(form.SlotMinutes)"` → параметр `Name`.

### Тесты

`NumberInputTest.cs` переписывается в тесты нового компонента
(файл `JoinInputNumberTest.cs`):
- мусорный ввод без EditContext → `null` (существующие кейсы сохраняются);
- `min`/`max`/`name` рендерятся атрибутами;
- в EditForm: непарсабельный ввод → validation error с `ParsingErrorMessage` (русский
  дефолт при незаданном параметре); валидный ввод → `NotifyFieldChanged` (валидация формы
  срабатывает); для decimal «1.5» — валидное значение, для int — ошибка;
- не-nullable `TValue` без EditContext при мусоре — `ValueChanged` не вызывается.

Механически правится `FormRowTest.cs` (`NumberInput`/`JoinInputNumber<int>` в тест-кейсах
перестанут компилироваться) — кейсы заменяются на новый компонент.

### Пакетные формальности (пакет IsPackable)

- `CHANGELOG.md` — запись в «Не выпущено»: удалён `NumberInput`, `JoinInputNumber`
  переписан (breaking: перестал наследовать `InputNumber<TValue>`).
- `PublicAPI.Shipped/Unshipped.txt` — перегенерировать `pwsh scripts/Update-PublicApi.ps1`.
- `README.md`, раздел «Стили» — `JoinInputNumber` перенести из Bootstrap-зависимых
  в список работающих без Bootstrap; `NumberInput` убрать.

### Демо

В WebComponentBook — демо-страница нового компонента по образцу существующих
(`Shared/JoinButtonDemo.razor`) и регистрация в `Shared/AllDemos.razor`, чтобы поле
появилось на `/no-bootstrap`.

## Проверка

- `dotnet build`, `dotnet format --verify-no-changes --severity warn`.
- `dotnet test src/Common/WebComponents/JoinRpg.Common.WebComponents.Test`.
- Визуально: WebComponentBook, страницы с демо компонента — с Bootstrap (Index) и на
  `/no-bootstrap`; формы, где живут переехавшие использования (расходы, бейджи, взносы,
  тип комнаты, тип персонажа, массовое добавление таймслотов).
