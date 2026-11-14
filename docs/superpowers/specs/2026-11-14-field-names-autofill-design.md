# Дизайн: осмысленные имена полей вместо голого `name="Name"`

Дата: 2026-11-14
Статус: предложено, ожидает одобрения

## Почему

Браузерное автодополнение (Chrome, Firefox) срабатывает на полях, не имеющих отношения
к персональным данным: «Название метода оплаты», «Название расхода» и т.п. Браузер
эвристикой ищет в атрибутах `name`/`id` токены вроде `name` и предлагает заполнить
поле именем из профиля; рядом с суммой и датой расхода комбинация принимается за форму
оплаты картой.

Причина в том, что все Blazor-формы рендерят `<input name="Name" id="Name">`: общие
компоненты ввода (`JoinTextInput`, `JoinTextArea`, `JoinInputNumber`, `JoinRpgDatePicker`,
`JoinRpgTimePicker`, `CheckboxInput`, `EnumSelector`, `EnumRadioButtonGroup`,
`TypedSelector`, `JoinUserLinkEditor`) берут имя поля из `FieldIdentifier.FieldName`,
то есть из голого имени свойства модели.

## Подход (выбран пользователем)

Переименовать генерируемый атрибут `name` так, чтобы он включал контекст формы:
`name="AddPaymentType.Name"` вместо голого `name="Name"`. Эвристика браузера перестаёт
распознавать в поле «имя человека», автодополнение не предлагается. Реализация —
единый компонент-обёртка `JoinEditForm` (префикс + гейт интерактивности), формы
переводятся на неё постепенно.
Варианты «централизованно autocomplete=off» и «точечные правки» рассмотрены и отклонены:
первый отключает полезное автодополнение целиком, второй не защищает будущие формы.

Паттерн «префикс.Имя» в проекте уже есть: `CharacterTypeSelector.razor` рендерит
`Name="@(Name + ".SlotName")"`.

## Ключевые решения

### Компонент-обёртка `JoinEditForm` (механика, выбрана при планировании)

Вместо точечного scope из диалога — общий компонент формы в `JoinRpg.Common.WebComponents`:

- `JoinEditForm` оборачивает `EditForm`: каскадирует `InputNameScope` с префиксом равным
  `FormName` и гейтит содержимое через `JoinInteractiveGate` (показывается только при
  работающей Blazor-интерактивности). Так префикс имён безопасен: нативный постинг
  пререндеренной формы с изменёнными именами становится невозможен, а это единственное,
  что мешало переименованию (форма настроек биндится через `[SupplyParameterFromForm]`
  по точному имени поля и до миграции на обёртку работала бы нативно — после миграции
  показывает заглушку загрузки до готовности WASM, как уже принято в `CreateProjectPanel`).
- Поля ввода с производными именами (`JoinTextInput`, `JoinTextArea`, `CheckboxInput`)
  при наличии области квалифицируют имя: `name="{FormName}.{ИмяСвойства}"`. Явный параметр
  `Name` по-прежнему побеждает (компоненты в постящихся MVC-формах — `Transfer/Create`,
  `Character/Edit`, `Claim/Edit`, `CharacterTypeSelector` — не меняются).
- `id` не меняем: внутри `FormRow` он уже генерируется как `formrow-{FieldName}-{guid}`.
- jquery-валидация (`data-valmsg-for`) считается из того же эффективного имени — консистентно.
- Не затрагиваются: компоненты с явным `Name` (`JoinRpgDatePicker`, `JoinInputNumber` и др.),
  `TypedSelector`, `JoinUserLinkEditor`, `EnumSelector`, `EnumRadioButtonGroup`.

### Миграция (постепенная)

В этом PR: `JoinFormDialog` (переводится на обёртку), 8 панелей `SettingsPanelBase`,
`MasterProfilePanel`, `AddMasterPanel`, `MasterPermissionsPanel`, `CreateProjectPanel`
(ручной гейт удаляется), `RoomTypeEditForm`, `BadgeTemplateEditForm`,
`ProjectRolesListForm`/`ProjectRolesListEditForm` — всем им добавляется/используется
`FormName`. Следующими PR: мелкие формы кнопок (`AcceptInvitationForm`, `ChangeFeeButton`,
`FinanceModerationButton`, `AddResponsibleMasterRuleForm`, `CaptainRuleForm`) — у них нет
триггерных полей. IdPortal не мигрирует: там автодополнение настоящее и нужное.

### Старые MVC-вью (4 формы)

### Старые MVC-вью (4 формы)

`GameGroups/AddGroup`, `GameGroups/Edit`, `GameField/Create`, `GameField/Edit` рендерят
`name="Name"` через `EditorFor` и постят в MVC-контроллеры, поэтому имя поля реально
участвует в model binding. Там: `ViewData.TemplateInfo.HtmlFieldPrefix` во вью +
`[Bind(Prefix = ...)]` на параметре экшена. Всего 4 формы.

## Non-goals

- Атрибут `autocomplete` не добавляем нигде (кроме существующих правильных токенов
  в IdPortal — их не трогаем).
- Парольные менеджеры (`data-1p-ignore` и т.п.) — отдельная тема, здесь не разбираем.

## Оставлено за реализатором (мелочи)

- Точный разделитель в id (`_` предполагается: `Name` с точкой в id недопустим).
- Нужно ли распространить префикс на `hidden`-поля `CheckboxInput`/`JoinUserLinkEditor`
  (скрытые поля не вызывают автодополнение, но консистентность имён не помешает).

## Проверка

- `dotnet build` + `dotnet format --verify-no-changes`.
- Юнит-тесты затронутых компонентов (`JoinRpg.Common.WebComponents.Test`): добавить/поправить
  проверки рендера `name`/`id`.
- Ручная проверка: диалоги «Добавить метод оплаты» и «Редактировать расход» больше не
  предлагают автозаполнение имени/карты; отправка форм и валидация работают как раньше.
