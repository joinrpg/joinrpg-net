# Отклонение одобренного расхода из грида

Дата: 2026-11-14. Статус: согласовано (подход A, права — CanManageMoney).

## Зачем

В гриде расходов (`/{projectId}/money/expenses`) кнопки «Одобрить»/«Отклонить» показываются
только для расходов в статусе `Proposed` (ExpensesPage.razor, `CanDecide`). Одобренный расход
нельзя отклонить из UI, хотя сервер это разрешает: `ExpenseService.ChangeState` требует только
`Permission.CanManageMoney` и не ограничивает переходы между состояниями.

Нужна: у одобренных расходов кнопка «Отклонить» для мастера с правом на деньги, выпадающая
из кнопки «Изменить» через `JoinDropdownButton`.

## Цели

- Мастер с `CanManageMoney` видит у одобренного расхода пункт «Отклонить» в выпадающем меню
  из кнопки «Изменить» (split-кнопка, как в `ListOperationsButtons.razor`).
- Действие спрашивает подтверждение и после успеха обновляет грид; ошибки показываются
  через `actionError`, как у существующих действий.
- Пункт меню с действием и подтверждением становится возможностью общего компонента
  `JoinMenuItem` (переиспользуемо).

## Не-цели

- Изменение серверных проверок прав (сервер уже готов).
- Отклонение расходов в других статусах, действия над отклонёнными расходами.
- Комментарий к отклонению (`comment: null`, как у существующих кнопок).

## Анти-цели

- Мутация через GET (пункт-ссылка на GET-endpoint отклонения).
- Кнопка у тех, у кого нет `CanManageMoney`.

## Подходы

- **A. Расширить `JoinMenuItem` + вынести общий код подтверждения** — выбран. Пункт меню
  с действием визуально остаётся строкой-ссылкой (CSS меню целится в `li > a`), а дублирующаяся
  проводка диалога подтверждения выносится во внутренний компонент `AutoConfirmDialog`,
  общий для `JoinButton` и `JoinMenuItem`. Хром кнопки (пресеты, спиннер, tooltip, рамка)
  в меню не переносится — он там вреден.
- **B. Кастомный пункт меню без изменения пакета** — отклонён: кнопка внутри ссылки —
  невалидная разметка, стили меню не применяются, подтверждение строится отдельно,
  не переиспользуется.
- **C. Научить `JoinDropdownButton` вкладывать `JoinButton` / дать `JoinButton` вид «ссылки»** —
  отклонён: конфликт scoped-CSS (JoinButton с OnClick рендерит `<button>`, под стили меню
  `li > a` не попадает; с Link получает и стили строки, и хром кнопки), нужен новый режим
  отображения в JoinButton — раздувание API кнопки заботами о меню.

## Изменения

1. `JoinRpg.Common.WebComponents/AutoConfirmDialog.razor` (новый, internal):
   внутренний компонент-обёртка над `JoinMessageDialog`. Параметры: `Caption`,
   сообщение (RenderFragment), OK-кнопка (`DialogButton`). Метод `Task<bool> ConfirmAsync()`.
   Убирает дубль проводки диалога из JoinButton.
2. `JoinRpg.Common.WebComponents/JoinButton.razor` — рефакторинг без изменения публичного
   API: блок `JoinMessageDialog` и `ShowBoolModalAsync` заменены на `AutoConfirmDialog`;
   OK-кнопка по-прежнему строится `DialogButton.FromJoinButton(this)`.
3. `JoinRpg.Common.WebComponents/JoinMenuItem.razor`:
   - `OnClick` (EventCallback<MouseEventArgs>), `AutoConfirm`, `AutoConfirmMessage`
     (зеркально JoinButton);
   - `OnClick` и `Href` несовместимы — исключение при обоих; при `AutoConfirm` сообщение
     обязательно;
   - `Href` перестаёт быть обязательным (сейчас `[EditorRequired]`), дефолт `""`;
   - при `OnClick` — `<a role="button" @onclick="...">` без `href` (CSS меню целится в
     `li > a`, стили строки и hover сохраняются);
   - `AutoConfirm` — рядом `AutoConfirmDialog`; OK-кнопка диалога — из `Label`, вариант
     задаётся новым опциональным параметром `ConfirmStyle` (для «Отклонить» — Danger).
4. `PublicAPI.Unshipped.txt` пакета — новые параметры JoinMenuItem (`AutoConfirm`,
   `AutoConfirmMessage`, `ConfirmStyle`, `OnClick`; ослабление `Href` с EditorRequired
   API не меняет).
5. `CHANGELOG.md` пакета — запись в «Не выпущено»: пункт меню с OnClick/подтверждением.
6. Тест `JoinRpg.Common.WebComponents.Test` — новый `JoinMenuItemTest.cs`: ссылка с Href;
   кнопка-ссылка с OnClick; AutoConfirm показывает диалог, отмена не вызывает OnClick,
   подтверждение вызывает. Плюс прогон существующих тестов JoinButton/JoinDropdownButton.
7. `JoinRpg.Blazor.Client/Expenses/ExpensesPage.razor`:
   - при `canManageMoney && expense.State == Approved` кнопка «Изменить» и
     `JoinDropdownButton Split="true"` (AriaLabel «Другие действия») уходят во вложенную
     `JoinButtonGroup`; в меню — `JoinMenuItem Label="Отклонить" ConfirmStyle="Danger"`
     с `AutoConfirmMessage` «Отклонить расход «{имя}»?» и OnClick в существующий
     `Decide(..., Client.RejectExpense)`;
   - комментарий к решению — `null`, как у текущих кнопок.
8. Тест `JoinRpg.Blazor.Client.Test/ExpensesPageTest.cs`: у Approved-расхода для
    canManageMoney-мастера есть пункт «Отклонить» в dropdown; у Proposed его нет
    (там обычные кнопки); у мастера без прав его нет.

## Решения, принятые в разговоре

- «Только админ» = мастер с правом на деньги (`CanManageMoney`), как для одобрения/отклонения
  предложенных расходов. Серверная проверка менять не нужно.
- Пункт меню один — «Отклонить»; «Потратил» в меню не переезжает.
- Диалог подтверждения — общий внутренний `AutoConfirmDialog` для JoinButton и JoinMenuItem
  (подход C «вкладывать JoinButton в меню» отклонён: конфликт стилей и структуры).
