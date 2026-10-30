# Как сделать регулярные джобы

Контракты джоб живут в пакете `JoinRpg.Common.WebInfrastructure.Interfaces` (namespace `JoinRpg.Common.WebInfrastructure.Job`), планировщик — в `JoinRpg.Common.WebInfrastructure`. Исторически инфраструктура называлась DailyJob и лежала в `JoinRpg.Interfaces` и `JoinRpg.Portal`; типы переименованы в `IJob`/`IJobRepository` и т.п., потому что планируется добавлять другие виды джобов (например, одноразовые миграции). Прод-контракты не менялись: секция конфига `DailyJobs`, строка подключения `"DailyJob"`, таблица `DailyJobRuns`.

## Джоба и регистрация

Джоба — класс с `IJob.RunOnce`. Регистрируется в своём модуле через `services.AddJob<T>()` (`Services.Impl`, `Services.Advertisement`, `Integrations.KogdaIgra`, Portal). `AddJob` регистрирует сам класс, раннер `JobRunner<TJob>` и маркер `RegisteredJob`; повторная регистрация того же типа падает при старте.

```csharp
using JoinRpg.Common.WebInfrastructure.Job;

[JobDelay(9)] // опционально: сдвиг планового запуска от полуночи UTC, в часах
class AdvertisementJob : IJob
{
    public Task RunOnce(CancellationToken cancellationToken) { /* ... */ }
}

services.AddJob<AdvertisementJob>();
```

Джоба обязана быть идемпотентной: если она вчера не отработала, должна сегодня доделать за вчера.

## Планировщик

Один hosted service на все джобы — `MidnightJobsScheduler`, регистрируется в composition root хоста (в Portal — `AddJoinJobs`):

```csharp
services.AddMidnightJobs(); // JoinRpg.Common.WebInfrastructure
```

На старте (после `WaitForAppStartup`) планировщик находит все зарегистрированные джобы по маркерам `IEnumerable<RegisteredJob>` — порядок вызовов `AddJob` относительно прочих регистраций не важен — и для каждой ведёт свой цикл: спит до примерно полуночи UTC плюс `JobDelay` (если атрибута нет — дефолтные 5 секунд), плюс случайный jitter 0–5 секунд, чтобы экземпляры не стартовали в ровный час; затем запускает джобу.

Запуск джобы — это `INSERT` в табличку `DailyJobRuns` (JobName, DayOfRun, JobStatus, MachineName), где `JobName` = полное имя класса, а `DayOfRun` — по UTC (иначе экземпляры с разной таймзоной запишут один запуск под разными днями, и блокировка «раз в сутки» не сработает).
- У таблички уникальный индекс на (JobName, DayOfRun); проигравший вставку экземпляр не запускает джобу.
- Успех — статус Completed, падение — Error. Все действия пишутся в лог; activity — источник `BackgroundJobService` с тегом `jobName` (подключается `AddJoinOpenTelemetry` автоматически, хосту ничего передавать не нужно).

Таким образом:
- Джоба запускается at most once в сутки
- Разные джобы могут быть запущены в параллель на разных экземплярах контейнера или одном
- Джоба может навсегда зависнуть в каком-то статусе (например, быть запущена и не завершиться). В этом случае она будет запущена на другой день
- Если контейнер будет штатно выключаться во время работы джобы, у неё 30 секунд, чтобы доделать свою работу

Джобы работают под роботом-администратором: раннер включает имперсонацию через `IJobImpersonator` (реализация `RobotJobImpersonator` в `JoinRpg.Services.Impl`) и снимает её после выполнения джобы.

## Как выключить джобу конфигом

Настройка `DailyJobs:Disabled` (класс `JobsOptions` в `JoinRpg.Common.WebInfrastructure.Interfaces`) — список выключенных джоб.
Имя — короткое (`AdvertisementJob`) или полное (`JoinRpg.Services.Advertisement.AdvertisementJob`) имя класса, без учёта регистра.
По умолчанию список пуст — работают все джобы.

```json
"DailyJobs": {
  "Disabled": [ "ProjectPerformCloseJob", "AdvertisementJob", "UpdatePaymentStatusJob" ]
}
```

Через переменные окружения (так на проде):

```
DailyJobs__Disabled__0=ProjectPerformCloseJob
DailyJobs__Disabled__1=AdvertisementJob
DailyJobs__Disabled__2=UpdatePaymentStatusJob
```

Что происходит с выключенной джобой:
- В плановое время планировщик её не запускает и **не пишет** строку в `DailyJobRuns`, в лог уходит Warning «disabled by configuration».
- На `/Admin/Jobs` у неё метка «Выключена в конфигурации», кнопка запуска неактивна, POST тоже отклоняется.
- При старте в лог пишется Warning со списком выключенных джоб; неизвестное имя в списке — тоже Warning с перечнем известных джоб (сайт не падает).

Настройка читается на каждом плановом запуске (`IOptionsMonitor`). Если источник конфига перезагружаемый
(`appsettings*.json`), включение и выключение работают без рестарта. Переменные окружения читаются только при старте — после их смены нужен рестарт пода.

После включения джоба отработает на ближайшем плановом запуске (полночь UTC плюс `JobDelay`), за пропущенные дни — согласно своей идемпотентности.
Если плановое время сегодня ещё не наступило и сервис его ждёт, джоба отработает уже сегодня. Немедленно запустить можно вручную с `/Admin/Jobs` (запуск через `IJobRunner` под роботом, без записи в `DailyJobRuns`).

Джобы регистрируются своими модулями через `AddJob<T>()` (см. выше); планировщик подключает хост (`AddMidnightJobs` в `AddJoinJobs` Portal). В IdPortal джоб нет.
