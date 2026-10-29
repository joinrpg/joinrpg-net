# Изменения JoinRpg.Common.WebComponents

Формат — по мотивам [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/).

## Не выпущено

### Ломающие изменения
- Из пакета убрана статика `_content/JoinRpg.Common.WebComponents/lib/bootstrap-select/`, `TypedSelector` она не нужна. Переход: если хост подключал её отсюда — подключить свою копию. ([#5867](https://github.com/joinrpg/joinrpg-net/pull/5867))

### Добавлено
- `JoinPager` — переключатель страниц: номера вокруг текущей, первая и последняя, «назад»/«вперёд». ([#5997](https://github.com/joinrpg/joinrpg-net/pull/5997))
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
