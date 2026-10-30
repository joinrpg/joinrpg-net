# JoinRpg.Common.WebInfrastructure.Interfaces

Контракты фоновых джоб Joinrpg — лёгкая зависимость для проектов, которые объявляют джобы,
но не должны тянуть весь пакет `JoinRpg.Common.WebInfrastructure` (Serilog, OpenTelemetry, EF Core).

```
dotnet add package JoinRpg.Common.WebInfrastructure.Interfaces
```

Нужен net10.0, всё лежит в `JoinRpg.Common.WebInfrastructure.Job`.

## Джоба и регистрация

```csharp
using JoinRpg.Common.WebInfrastructure.Job;

[JobDelay(9)] // опционально: сдвиг планового запуска от полуночи UTC, в часах
class AdvertisementJob : IJob
{
    public Task RunOnce(CancellationToken cancellationToken) { /* ... */ }
}

services.AddJob<AdvertisementJob>();
```

`AddJob` регистрирует сам класс, раннер (создаётся фабрикой `IJobRunnerFactory`, реализация —
в WebInfrastructure) и маркер `RegisteredJob`. Планировщик (полночь UTC, at-most-once в сутки,
журнал через `IJobRepository`) — `AddMidnightJobs` из пакета `JoinRpg.Common.WebInfrastructure`:
он сам находит все зарегистрированные джобы по маркерам при старте, порядок вызовов `AddJob`
не важен.

Джоба обязана быть идемпотентной: если она вчера не отработала, сегодня должна доделать за вчера.
Выключение конфигом — секция `DailyJobs:Disabled` (короткое или полное имя класса), см.
`JobsOptions`.

## Типы

- `IJob` + `JobDelayAttribute` — контракт джобы.
- `AddJob<TJob>()` — регистрация.
- `IJobRunner` — запуск джобы в переданном scope под имперсонацией (для админской страницы и тестов); реализация раннера — в `JoinRpg.Common.WebInfrastructure`.
- `IJobRunnerFactory` — фабрика раннеров; реализацию регистрирует `AddMidnightJobs`.
- `IJobImpersonator` — под кем выполняются джобы; реализует хост-приложение (в Joinrpg Portal — робот-администратор).
- `IJobRepository` + `JobId` — журнал «один запуск в сутки» (уникальный индекс JobName + DayOfRun).
- `JobsOptions` + `RegisteredJob` — настройка `DailyJobs:Disabled` и маркер зарегистрированной джобы.

Как это устроено целиком — [ADR002](https://github.com/joinrpg/joinrpg-net/blob/master/docs/adr002-dailyjobs.md).
