# Изменения JoinRpg.Common.WebInfrastructure.Interfaces

Формат — по мотивам [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/).

## Не выпущено

### Добавлено
- Первый выпуск: контракты фоновых джоб — `IJob`, `JobDelayAttribute`, `IJobRepository`, `JobId`, `IJobRunner`, `IJobRunnerFactory`, `RegisteredJob`, `JobsOptions` (секция `DailyJobs`), `IJobImpersonator` и регистрация `AddJob<TJob>()`. Выделены из `JoinRpg.Interfaces` (`IDailyJob`) и `JoinRpg.Common.WebInfrastructure` (`IDailyJobRepository`); реализация раннера — в `JoinRpg.Common.WebInfrastructure`.
