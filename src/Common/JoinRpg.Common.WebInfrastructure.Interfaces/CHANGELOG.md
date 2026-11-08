# Изменения JoinRpg.Common.WebInfrastructure.Interfaces

Формат — по мотивам [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/).

## Не выпущено

### Добавлено
- Контракты HTML-санитайзеров (`IJoinHtmlSanitizer`: `Sanitize` + `IDisposable`, и профили `IJoinSimpleHtmlSanitizer`, `IJoinRemoveAllHtmlSanitizer`, `IJoinTelegramHtmlSanitizer`; namespace `HtmlSanitize`). Реализация (`AddJoinHtmlSanitizers`, пул `ObjectPool`) — в `JoinRpg.Common.WebInfrastructure`. Временный Obsolete-`JoinHtmlSanitizerLocator` связывает регистрацию со статическими вызовами до перевода рендеринга markdown на DI. ([#6164](https://github.com/joinrpg/joinrpg-net/pull/6164))

## 2026.9.651 — 2026-11-07

### Добавлено
- Первый выпуск: контракты фоновых джоб — `IJob`, `JobDelayAttribute`, `IJobRepository`, `JobId`, `IJobRunner`, `IJobRunnerFactory`, `RegisteredJob`, `JobsOptions` (секция `DailyJobs`), `IJobImpersonator` и регистрация `AddJob<TJob>()`. Выделены из `JoinRpg.Interfaces` (`IDailyJob`) и `JoinRpg.Common.WebInfrastructure` (`IDailyJobRepository`); реализация раннера — в `JoinRpg.Common.WebInfrastructure`.
