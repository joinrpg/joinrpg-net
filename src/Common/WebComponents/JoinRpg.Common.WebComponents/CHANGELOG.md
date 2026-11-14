# Изменения JoinRpg.Common.WebComponents

Формат — по мотивам [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/).

## Не выпущено

### Ломающие изменения
- `PriceElement`: параметр `Price` — теперь `decimal` (был `int`). Деньги с копейками; копейки показываются только если есть («1500», «49,9»). Переход: передать `decimal` (целые значения преобразуются неявно).

### Добавлено
- `AutoConfirmDialog` — общий компонент подтверждающего диалога для компонентов с действием и `AutoConfirm` (JoinButton, TypedDropdownButton). Параметр `Type` — тип сообщения диалога (по умолчанию `Confirmation`).
- `JoinInteractiveGate` — обертка для содержимого WASM-островов с вводом: показывает его только после загрузки WebAssembly. До загрузки WASM пререндеренный остров выглядит живым, но Blazor не исполняется — ввод обрабатывает только делегированный JS и при гидратации острова тихо стирается.
- `JoinEditForm` — обёртка над `EditForm`: гейтит содержимое до загрузки WebAssembly (`JoinInteractiveGate`) и каскадирует область имён полей — поля ввода с производным именем получают `name` с префиксом `FormName` (например, `name="AddPaymentType.Name"` вместо `name="Name"`), что убирает ложные срабатывания браузерного автодополнения. Явный параметр `Name` у полей по-прежнему побеждает; формы вне `JoinEditForm`/`JoinFormDialog` не меняются.
- `JoinRpgDatePicker`, `JoinRpgTimePicker`: параметр `ValueExpression` — при заданном выражении компонент уведомляет `EditContext` об изменении поля формы (каскадный параметр `EditContext`): сообщение валидации гаснет, а кнопка отправки разблокируется без перерисовки родителя.

### Изменено
- `JoinFormDialog` переведён на `JoinEditForm`: имена полей диалогов получают префикс `FormName`.

### Исправлено
- `EnumRadioButtonGroup`: `SelectedValueExpression` — необязательный параметр (как и задокументировано). Раньше компонент падал при рендере без выражения; теперь уведомляет `EditContext` об изменении, только если выражение задано.

## 2026.9.668 — 2026-11-08

### Ломающие изменения
- Удалён `NumberInput` — заменён на `JoinInputNumber<TValue>`. Переход: использовать `JoinInputNumber<TValue>`. ([#6166](https://github.com/joinrpg/joinrpg-net/pull/6166))
- `JoinInputNumber<TValue>` переписан — больше не наследует `InputNumber<TValue>`; дефолтное сообщение об ошибке парсинга — русское «Укажите число» (был английский ресурс `InputNumber`); поле больше не получает Bootstrap-класс `form-control` (оформление — собственный scoped CSS, компонент работает без Bootstrap). Переход: не рассчитывать на члены базового `InputNumber<TValue>`; своё сообщение — параметром `ParsingErrorMessage`. ([#6166](https://github.com/joinrpg/joinrpg-net/pull/6166))
- Удалён `StringInput` — заменён на `JoinTextInput`. Переход: использовать `JoinTextInput` (`Name` вместо lowercase-атрибута `name`, пустой/пробельный ввод уходит наружу как `""`, а не `null` — серверные проверки на пустоту ведут себя как раньше). ([#6169](https://github.com/joinrpg/joinrpg-net/pull/6169))

### Добавлено
- `JoinTextArea`, `JoinMarkdownTextArea`: параметры `MaxLength` (рендерит `maxlength`) и `ShowCounter` — счётчик «текущая/максимальная» длина мелким текстом под полем, обновляется при наборе. ([#6177](https://github.com/joinrpg/joinrpg-net/pull/6177))
- `JoinTextInput`: параметр `Variants` — варианты значений для подсказки ввода (datalist). ([#6156](https://github.com/joinrpg/joinrpg-net/pull/6156))

### Изменено
- `JoinRpgDatePicker`: поле больше не получает Bootstrap-класс `form-control` (оформление — собственный scoped CSS, компонент работает без Bootstrap). ([#6140](https://github.com/joinrpg/joinrpg-net/issues/6140))
- `JoinTextInput`: поле больше не получает Bootstrap-класс `form-control` (оформление — собственный scoped CSS, span обязательности — scoped CSS вместо Bootstrap `text-danger`, компонент работает без Bootstrap); параметры `Name`, `Required`, `RequiredMessage` — перенесены из удалённого `StringInput` (клиентская валидация обязательности через `data-val-*` сохраняется). ([#6169](https://github.com/joinrpg/joinrpg-net/pull/6169))
- `JoinDialog`: контент компенсирует отрицательные margin'ы Bootstrap-строк (`.form-horizontal .form-group`, `.row`) — формы в диалоге больше не дают горизонтальную прокрутку окна, локальные обёртки `overflow-x: hidden` у форм в диалогах больше не нужны. ([#6156](https://github.com/joinrpg/joinrpg-net/pull/6156))

### Исправлено
- `JoinInputNumber<TValue>`: атрибуты `min`/`max` рендерятся, только когда переданы параметры `Min`/`Max`. Раньше для ненуллимого `TValue` (например, `int`) непереданные границы рендерились как `min="0" max="0"` и блокировали ввод положительных значений. ([#6171](https://github.com/joinrpg/joinrpg-net/pull/6171))
- `JoinDialog`: `ShowModal()` из продолжения после `ShowModalAsync`/`ShowBoolModalAsync` снова открывает окно — раньше без SynchronizationContext (например, в тестах) срабатывал встроенный guard и окно не открывалось. В продакшен-приложениях Blazor поведение не меняется. ([#6156](https://github.com/joinrpg/joinrpg-net/pull/6156))

## 2026.9.651 — 2026-11-07

### Ломающие изменения
- Удалён `ElementMoveCommandViewModel` — модель POST-команды для `JoinMoveControlNonInteractive`, вне joinrpg не использовалась. Переход: не требуется. ([#6042](https://github.com/joinrpg/joinrpg-net/pull/6042))

### Добавлено
- `ButtonPreset.Archive` — кнопка «В архив» (Warning) с новой иконкой `JoinIconType.Archive`. ([#6157](https://github.com/joinrpg/joinrpg-net/pull/6157))
- `EnumRadioButtonGroup`: параметр `Values` — какие значения enum показывать (по умолчанию все). ([#6068](https://github.com/joinrpg/joinrpg-net/pull/6068))
- Кнопка с `data-join-collapse-close` сворачивает и ближайший голый `<details>`, не только `JoinCollapsePanel` — для лёгких тогглов вроде формы ответа в обсуждениях. ([#6053](https://github.com/joinrpg/joinrpg-net/pull/6053))
- `JoinBadge`: параметр `TooltipContent` — подсказка бейджа разметкой, когда простой строки `Title` мало. ([#6059](https://github.com/joinrpg/joinrpg-net/pull/6059))
- `JoinPager` — переключатель страниц: номера вокруг текущей, первая и последняя, «назад»/«вперёд». ([#5997](https://github.com/joinrpg/joinrpg-net/pull/5997))
- `JoinDropdownButton`: параметры `Style`, `Size` (шкала `JoinButton`, размер приходит и из `JoinButtonGroup`) и `ToggleContent` — своё содержимое кнопки вместо подписи и стрелки. ([#6018](https://github.com/joinrpg/joinrpg-net/pull/6018))

### Устарело
- `JoinMoveControlNonInteractive`: перестановка с перезагрузкой страницы ушла на `JoinMoveControl` + `IMoveClient` (`webapi/move/moveafter`). Удалится в следующей мажорной версии. ([#6042](https://github.com/joinrpg/joinrpg-net/pull/6042))

### Изменено
- `JoinAlert`: собственные классы `join-alert`/`join-alert--*` и scoped CSS пакета вместо классов Bootstrap (`alert`/`alert-*`) — алерт выглядит как раньше и без подключённого Bootstrap CSS. Вместе с ним без Bootstrap работают рендерящие его `JoinLoadingMessage` и `JoinErrorMessage`. Цвета вариаций берутся из общей палитры пакета (`css/variations.css`), вариация `Primary` получила цвета Bootstrap 5 — при классах Bootstrap 3 класс `alert-primary` был мёртвым и алерт выглядел без цвета. Переход: если стилизовали алерты снаружи по классам Bootstrap — поправьте селекторы на `join-alert`. ([#6151](https://github.com/joinrpg/joinrpg-net/pull/6151))
- `JoinCollapsePanel`: оформление — свои классы (`join-collapse-panel--*`, `join-collapse-panel__heading`, `join-collapse-panel__body`) и стили пакета вместо классов Bootstrap 3 (`panel panel-*`, `panel-heading`, `panel-body`). Цвета вариаций — из общей палитры пакета `css/variations.css` (фасеты `panel-*`). `JoinCollapsePanelGroup` рендерит `join-collapse-panel-group` вместо `panel-group`. Переход: если стилизовали панель снаружи через `.panel*` — перенесите правила на `join-collapse-panel*`. ([#6127](https://github.com/joinrpg/joinrpg-net/issues/6127))
- `JoinPanel`: оформление — свои классы (`join-panel`, `join-panel--*`, `join-panel__heading`, `join-panel__body`, `join-panel__footer`) и scoped CSS пакета вместо классов Bootstrap 3 (`panel panel-*`, `panel-heading`, `panel-body`, `panel-footer`). Цвета вариаций — из общей палитры пакета `css/variations.css` (фасеты `panel-*`). Рендерящие его `SupportInfoPanel` и `VersionPanel` выигрывают автоматически. Переход: если стилизовали панель снаружи через `.panel*` — перенесите правила на `join-panel*`. ([#6129](https://github.com/joinrpg/joinrpg-net/issues/6129))
- `JoinDotsMenu` и `TypedDropdownButton`: меню открывается нативным `<details>` (как `JoinDropdownButton`), а не JS Bootstrap 3 (`data-toggle="dropdown"`). Без JS Bootstrap меню больше не «молча не открывается». Переход: разметка внутри — `details/summary`, а не `span.dropdown`/`button` — поправьте селекторы, если стилизовали меню снаружи. ([#6018](https://github.com/joinrpg/joinrpg-net/pull/6018))
- Цвета вариаций (`default`, `primary`, `success`, `info`, `warning`, `danger`) вынесены в общий файл пакета `css/variations.css`: `JoinBadge`, `JoinButton`, `JoinDropdownButton` берут их через CSS-переменные. Хосту нужен один статический CSS пакета — `css/join-components.css` (палитра + иконки), см. README. ([#6150](https://github.com/joinrpg/joinrpg-net/pull/6150))

### Исправлено
- `FormRow` без `Label`: вместо пустого `<label>` — пустая колонка, `Label` теперь `string?`. ([#6010](https://github.com/joinrpg/joinrpg-net/pull/6010))
- `JoinBadge`: параметр `Size` заработал — классы `label-xs/label-sm/label-lg` определены в пакете (Bootstrap 3 не имеет размеров у `.label`, раньше параметр молча игнорировался). Бейджи с заданным `Size` изменят размер. ([#6085](https://github.com/joinrpg/joinrpg-net/pull/6085))
- `JoinBadge`: базовое оформление и цвета всех `.label-*` теперь в пакете — без подключённого Bootstrap бейджи больше не бесцветные, внешнему потребителю не нужно определять `.label-*` у себя. ([#6150](https://github.com/joinrpg/joinrpg-net/pull/6150))

## 2026.9.533 — 2026-10-29

### Ломающие изменения
- Из пакета убрана статика `_content/JoinRpg.Common.WebComponents/lib/bootstrap-select/`, `TypedSelector` она не нужна. Переход: если хост подключал её отсюда — подключить свою копию. ([#5867](https://github.com/joinrpg/joinrpg-net/pull/5867))

### Добавлено
- `SelectorListCache` и `AddJoinRpgWebComponents()`: общий на scope кэш списков для обёрток над `TypedSelector`, списки с пререндера переезжают в WebAssembly. ([#5880](https://github.com/joinrpg/joinrpg-net/pull/5880), [#5895](https://github.com/joinrpg/joinrpg-net/pull/5895))
- `FormRowLabelTarget.LabelId` — id подписи `FormRow`. ([#5866](https://github.com/joinrpg/joinrpg-net/pull/5866))
- `JoinCollapsePanel`: кнопка с `data-join-collapse-close` в статической форме сворачивает панель. ([#5940](https://github.com/joinrpg/joinrpg-net/pull/5940))
- `UserLink.ShowIcon` — `false` убирает иконку-человечка, когда рядом уже аватарка. ([#5957](https://github.com/joinrpg/joinrpg-net/pull/5957))
- Пакет: README.md и CHANGELOG.md, лицензия MIT. ([#5948](https://github.com/joinrpg/joinrpg-net/pull/5948), [#5953](https://github.com/joinrpg/joinrpg-net/pull/5953))

### Изменено
- `TypedSelector` в `FormRow`: подпись связана со списком (клик раскрывает, `aria-labelledby`). ([#5866](https://github.com/joinrpg/joinrpg-net/pull/5866))

### Исправлено
- `TypedSelector`: в `<fieldset disabled>` не раскрывается. ([#5874](https://github.com/joinrpg/joinrpg-net/pull/5874))
- `FormRow`: id поля уникален и при статической строке рядом с островом. ([#5900](https://github.com/joinrpg/joinrpg-net/pull/5900))
- `CheckboxInput`: галочка постится формой и до загрузки WASM, отключённая постит текущее значение. ([#5923](https://github.com/joinrpg/joinrpg-net/pull/5923))
- `TypedSelector` в модальном `<dialog>`: клик по тексту пункта выбирает его — раньше список закрывался на нажатии, и срабатывал только клик точно по галочке. ([#5967](https://github.com/joinrpg/joinrpg-net/pull/5967))

## 2026.9.438

### Ломающие изменения
- `TypedSelector`: `<details>` с radio/checkbox вместо bootstrap-select; без выбранного значения поле не постится (раньше — первый пункт). Переход: обработчик формы проверяет, что значение пришло. ([#5864](https://github.com/joinrpg/joinrpg-net/pull/5864))
- `TypedSelector.Width`: CSS-ширина, по умолчанию 220px, `"auto"` больше не подгоняет ширину. Переход: задать ширину явно. ([#5864](https://github.com/joinrpg/joinrpg-net/pull/5864))
- `JoinTextInput`: обрезает пробелы по краям. Переход: `Trim="false"`, если они нужны. ([#5877](https://github.com/joinrpg/joinrpg-net/pull/5877))
- `SupportInfoPanel`: убран параметр `CommitUrl`. Переход: не передавать его. ([#5860](https://github.com/joinrpg/joinrpg-net/pull/5860))

### Добавлено
- `JoinDropdownButton` и `JoinMenuItem`: кнопка с выпадающим меню, работает и без интерактива; режим `Split` в `JoinButtonGroup`. ([#5802](https://github.com/joinrpg/joinrpg-net/pull/5802), [#5869](https://github.com/joinrpg/joinrpg-net/pull/5869))
- `JoinMarkdownTextArea`: `JoinTextArea` с подсказкой про Markdown. ([#5849](https://github.com/joinrpg/joinrpg-net/pull/5849), [#5857](https://github.com/joinrpg/joinrpg-net/pull/5857))
- `PrintableBadge`, `PrintableBadgeLine`: бейдж для печати (размеры в мм, фон, строки с подгонкой шрифта); статической странице нужен скрипт `printable-badge.js`. ([#5789](https://github.com/joinrpg/joinrpg-net/pull/5789), [#5814](https://github.com/joinrpg/joinrpg-net/pull/5814))

### Изменено
- `SettingsPanelBase`: серверная ошибка — на модели целиком, выводится `<ValidationSummary Model="Model" />`. ([#5858](https://github.com/joinrpg/joinrpg-net/pull/5858))
- Зависимости `Microsoft.AspNetCore.Components`, `.Authorization`, `.Web` и `Microsoft.Extensions.DependencyInjection.Abstractions` — 10.0.14. ([#5868](https://github.com/joinrpg/joinrpg-net/pull/5868))

### Исправлено
- `EnumSelector`: отмечает выбранное значение. ([#5805](https://github.com/joinrpg/joinrpg-net/pull/5805))
- `JoinDialog`: `FooterContent` рендерится и без `Buttons`. ([#5831](https://github.com/joinrpg/joinrpg-net/pull/5831))
- `JoinCollapsePanel`: якорь раскрывает панель и без prerender, заголовок не прячется под шапку. ([#5842](https://github.com/joinrpg/joinrpg-net/pull/5842))

## 2026.9.363

### Ломающие изменения
- `JoinTableSimple`: по умолчанию в прокручиваемой обёртке, она обрезает выпадающие меню в ячейках. Переход: `Responsive="false"` для таких таблиц. ([#5717](https://github.com/joinrpg/joinrpg-net/pull/5717))

### Добавлено
- `ServerProblemException`, `EnsureSuccessOrThrowProblem()`, `ReadFromJsonOrThrowProblem<T>()`: отказ webapi (4xx) с текстом для пользователя. ([#5606](https://github.com/joinrpg/joinrpg-net/pull/5606), [#5741](https://github.com/joinrpg/joinrpg-net/pull/5741))
- `SupportInfoPanel`: данные для техподдержки на странице ошибки. ([#5614](https://github.com/joinrpg/joinrpg-net/pull/5614))
- `VersionPanel`: версия и ссылка на коммит. ([#5614](https://github.com/joinrpg/joinrpg-net/pull/5614))
- `Tooltip`: параметр `Block` — цель растянута на ширину родителя. ([#5767](https://github.com/joinrpg/joinrpg-net/pull/5767))
- `ContactLink`, `EmailLink`: параметр `Collapse` — только иконка. ([#5686](https://github.com/joinrpg/joinrpg-net/pull/5686))

### Изменено
- `SettingsPanelBase`, `JoinUserLinkEditor`, `JoinMoveControl`: показывают текст `ServerProblemException`; `JoinMoveControl` — диалогом, не бросая исключение. ([#5606](https://github.com/joinrpg/joinrpg-net/pull/5606), [#5748](https://github.com/joinrpg/joinrpg-net/pull/5748), [#5751](https://github.com/joinrpg/joinrpg-net/pull/5751))
- `PhoneLink`: номер — ссылка `tel:`. ([#5686](https://github.com/joinrpg/joinrpg-net/pull/5686))

### Исправлено
- `DialogButton.FromJoinButton`: кнопка-иконка с `AutoConfirm` не падает при рендере. ([#5700](https://github.com/joinrpg/joinrpg-net/pull/5700))

Более ранние версии — [история коммитов](https://github.com/joinrpg/joinrpg-net/commits/master/src/Common/WebComponents/JoinRpg.Common.WebComponents).
