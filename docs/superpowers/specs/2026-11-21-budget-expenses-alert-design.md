# Дизайн: алерт о давно отсутствующих расходах в проекте бюджета JoinRpg

Дата: 2026-11-21
Статус: на согласовании

## Намерение и зачем

Команда JoinRpg ведёт внутренний бюджет (аренда, железо, сервисы) как расходы
в выделенном проекте («проект бюджета», сейчас — donate-проект, на проде id 183).
Если фактические расходы долго не вносятся, бюджет «протухает»: данные не отражают
реальность, а отчётность по расходам теряет смысл.

Фича: автоматический алерт. Раз в неделю (по вторникам) проверяем, была ли хотя бы
одна фактическая трата в предыдущем календарном месяце в проекте бюджета. Если не
было — уведомляем всех финансовых админов этого проекта.

## Цели

- Финансовые админы проекта бюджета регулярно (раз в неделю) получают сигнал,
  если расходы за прошлый месяц не внесены.
- Проверка бесплатная по ресурсам: один `Any()` в БД раз в неделю; вне вторника
  расходы даже не читаются.

## Не-цели

- Никаких автоматических действий с проектом или деньгами — только уведомление.
- Проверка других проектов (кроме проекта бюджета) не нужна.
- Настраиваемый порог/расписание через UI не нужно — константы в коде.

## Анти-цели

- Спам ограничен еженедельным расписанием: алерт приходит не чаще раза в неделю
  и не приходит, если расходы вносятся. Пока расходы за прошлый месяц не внесены,
  алерт повторяется каждый вторник — это осознанное поведение.
- Ложный алерт из-за расхода без даты: трата внесена, а дата не проставлена —
  админ увидит письмо и либо проставит дату, либо поймёт, что алерт легитимен.

## Ограничения и правила проекта (AGENTS.md)

- Джобы — паттерн ADR002: `IJob`, идемпотентность, `AddJob<T>()`, `[JobDelay]`,
  планировщик только ежедневный — день недели проверяет сама джоба
  (образец: `BlockedProjectsReminderJob`).
- Видимые пользователю строки — на русском, в слое UI/шаблонов писем.
- DI — явная регистрация в `AddJoinDomainServices` (`JoinRpg.Services.Impl/Services.cs`),
  Autofac `GetTypes()` не пополнять.
- Без regex. Файлы `.cs` править только редактором (CRLF).
- Новых полей в DataModel нет — согласование с @leotsarev не требуется.
- При фиксах/фичах — юнит-тесты в существующих тестовых проектах.

## Решения, принятые в обсуждении

1. **Переименование `DonateOptions` → бюджетные опции.** Тип `DonateOptions`
   (`JoinRpg.Portal/Menu/DonateOptions.cs`, секция `"Donate"`) — единственное место,
   где задан проект бюджета. Переименовываем в `BudgetOptions` (имя типа решено
   в реализации, вариант: `JoinRpgBudgetOptions`), переносим в сборку, доступную
   сервисному слою, секция конфига — `"Budget"`. Старый `DonateOptions` и секция
   `"Donate"` удаляются; страница `Donate.cshtml.cs` использует новый тип.
2. **Критерий фактического расхода:** `Expense.State == Approved` &&
   `ActualQuantity != null` && `SpentAt` внутри предыдущего календарного месяца
   (полуинтервал [первое число прошлого месяца, первое число текущего)).
   Даты `UpdatedAt`/даты одобрения не используем, fallback нет
   (`SpentAt` может быть null у одобренного расхода — тогда он не засчитывается,
   это осознанный риск ложного алерта, см. анти-цели).
3. **Каналы:** обычный `QueueNotification(event)` — все каналы по настройкам
   каждого админа (email, Telegram, in-app).
4. **Prod/dev конфиг — через `.env` в `manifests/`.** Значение добавляется в
   закоммиченные `manifests/prod/.env` (`Budget__ProjectId=183`) и
   `manifests/dev/.env` (`Budget__ProjectId=337`, старая строка
   `Donate__DonateProjectId` временно сохраняется). Подача в под: `envFrom`:
   сначала secret `joinrpg-main` (GitHub Environment secrets), затем
   configMap из `.env` — при совпадении ключей побеждает configMap. Старый
   секрет `Donate__DonateProjectId` в GitHub Environment удаляется вручную
   после деплоя; старая строка в `manifests/dev/.env` — cleanup-PR.
5. **Неактивный проект = алерта нет.** Если проект бюджета заблокирован/
   закрыт (`ProjectLifecycleStatus`), джоба молча завершается (по образцу
   `BlockedProjectsReminderJob`).
6. **Выделить сервис рассылок мастерам.** Из `MasterEmailService` выделяется
   отдельный сервис (условно `MastersNotificationService`, имя — в реализации)
   с тремя сценариями получателей: «все мастера проекта», «мастера с заданным
   `Permission`», «мастера + админы сайта» (с дедупликацией, как сейчас в
   `EmailProjectUnblocked`). Общая логика отправки (шаблон
   «Добрый день, %recepient.name%!», `NotificationClass.MasterProject`,
   `QueueNotification`) живёт в нём. `MasterEmailService` оставляет за собой
   только составление темы/текста и делегирует выбор получателей и отправку.
   Новая фича использует сценарий «мастера с `Permission.CanManageMoney`».

## Подходы (как строить)

Рассмотрено:

- **A. По образцу `BlockedProjectsReminderJob` + `MasterEmailService` (выбран).**
  Ежедневная джоба с проверкой `DayOfWeek.Tuesday`, проверка через новый метод
  `IExpenseRepository`, уведомление — новый метод в `MasterEmailService` с
  фильтром `Masters.Where(CanManageMoney)`. Полностью повторяет существующие
  паттерны репозитория, раскладка по проектам согласно `docs/structure.md`.
- B. Отдельный сервис-алерт со своей абстракцией над уведомлениями. Отвергнуто:
  избыточно для одного проекта и одного письма, дублирует `MasterEmailService`.
- C. Переиспользовать `FinanceOperation` (взносы) как сигнал активности.
  Отвергнуто: взносы — доходы игроков, не расходы команды; семантика не та.

## Как устроено (что трогаем)

1. **`BudgetOptions`** — новый тип опций (поле `ProjectId` типа `ProjectIdentification`),
   сборка — сервисная (`JoinRpg.Services.Impl` или контракты, решить в реализации
   по принципу «кто использует»; используют джоба и страница донатов в портале).
   Конфиг: секция `"Budget": { "ProjectId": 183 }` в `appsettings.json` портала
   и в конфиге окружений (`manifests/*/​.env` как `Budget__ProjectId`, см.
   решение 4). `IOptions<BudgetOptions>` инжектится в джобу.
   Старый `DonateOptions`/секция `"Donate"` удаляются сразу в PR1 — атомарный
   переход без промежуточного мёртвого кода (уточнено по ревью PR1).
2. **Репозиторий.** В `IExpenseRepository` (`src/JoinRpg.Data.Interfaces/Finances/`)
   новый метод вида `Task<bool> HasActualExpensesInPeriod(ProjectIdentification projectId, DateTime fromUtc, DateTime toUtc)`
   с реализацией `Any()` в `ExpenseRepositoryImpl` по критерию из решения 2.
   LINQ через `Expression`/LinqKit по `docs/linq-queries.md`.
3. **Джоба `BudgetExpensesAlertJob : IJob`** в `JoinRpg.Services.Impl` (папка по
   смыслу — рядом с джобами проектов или финансов). По образцу `BlockedProjectsReminderJob`:
   `[JobDelay(...)]`, `TimeProvider`. Каждый день: если сегодня не вторник —
   выходим; иначе: нет расходов за прошлый календарный месяц → уведомление.
   Идемпотентна (at-most-once
   в сутки гарантирует планировщик; повторный запуск в тот же день безвреден —
   просто ещё одно письмо, как у существующих рассылок).
4. **Регистрация:** `AddJob<BudgetExpensesAlertJob>()` в `AddJoinDomainServices`
   (`src/JoinRpg.Services.Impl/Services.cs`).
5. **`MasterEmailService`** (`src/JoinRpg.Services.Impl/Projects/MasterEmailService.cs`):
   новый метод `EmailBudgetExpensesAlert(...)` рядом с
   `EmailProjectBlockedReminder`: тема/текст на русском, в тексте — ссылка на
   страницу расходов проекта и период (прошлый месяц); получатели и отправка —
   через новый сервис из решения 6 (сценарий «мастера с правом»).
   Initiator — `RobotUserId`.
6. **`MastersNotificationService`** (новый, выделен в PR2): методы
   `NotifyAllMasters`, `NotifyMastersWithPermission(project, permission, ...)`,
   `NotifyMastersAndAdmins`. Существующие методы `MasterEmailService`
   переводятся на него без изменения поведения (рефакторинг отдельным PR).
6. **Тесты:** юнит-тесты в `JoinRpg.Services.Impl.Test` — логика джобы
   (вторник/не вторник, есть/нет расходов за прошлый месяц → письмо/нет) по образцу тестов
   `BlockedProjectsReminderJob`/`MasterEmailService`; тест репозитория не пишем
   (простой `Any()`), при желании — тест маппинга критерия.

## Что оставлено на реализатора (мои решения, подлежат проверке)

- Точное имя типа опций (`BudgetOptions`) и его сборка: вынести в контракты,
  если портал и сервисы оба должны видеть тип без циклических ссылок; секция
  конфига — `"Budget"`.
- Значение `[JobDelay]` — как у `BlockedProjectsReminderJob` (9), чтобы письмо
  приходило утром по Москве.
- Название джобы и папка — по аналогии с соседями.
- Текст письма (русский) — по аналогии с `EmailProjectBlockedReminder`.

## Порядок PR

1. **PR1 (в работе у субагента):** переименование `DonateOptions` → `BudgetOptions` + конфиг:
   код портала, `appsettings.json` (секция `"Budget"`), новая строка
   `Budget__ProjectId=183` в `manifests/prod/.env`, строка
   `Budget__ProjectId=337` в `manifests/dev/.env` (рядом со старой,
   которая остаётся до cleanup). Старый секрет `Donate__DonateProjectId`
   в GitHub Environment после деплоя удаляется вручную (@leotsarev).
2. **PR2 (рефакторинг):** выделение `MastersNotificationService` из
   `MasterEmailService` — три сценария получателей, поведение не меняется.
3. **PR3 (фича):** метод `HasActualExpensesInPeriod` в `IExpenseRepository`,
   джоба в `JoinRpg.Services.Impl` (проверка вторника, активность проекта,
   наличие расходов за прошлый календарный месяц), `AddJob<>()` в
   `Services.cs`, метод
   `EmailBudgetExpensesAlert` в `MasterEmailService`, юнит-тесты по образцу
   `BlockedProjectsReminderJobTest`.
4. **PR4 (cleanup):** удаление строки `Donate__DonateProjectId` из
   `manifests/dev/.env` (тип `DonateOptions` и секция `"Donate"` удалены ещё
   в PR1). Удаление секрета `Donate__DonateProjectId` из GitHub Environment —
   вручную, вне репозитория.

Перед каждым PR — ревью субагентом и `dotnet format`, по AGENTS.md.
