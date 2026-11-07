# Изменения JoinRpg.Common.WebInfrastructure.Interfaces

Формат — по мотивам [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/).

## Не выпущено

## 2026.9.651 — 2026-11-07

### Добавлено
- Первый выпуск: контракты фоновых джоб — `IJob`, `JobDelayAttribute`, `IJobRepository`, `JobId`, `IJobRunner`, `IJobRunnerFactory`, `RegisteredJob`, `JobsOptions` (секция `DailyJobs`), `IJobImpersonator` и регистрация `AddJob<TJob>()`. Выделены из `JoinRpg.Interfaces` (`IDailyJob`) и `JoinRpg.Common.WebInfrastructure` (`IDailyJobRepository`); реализация раннера — в `JoinRpg.Common.WebInfrastructure`.
