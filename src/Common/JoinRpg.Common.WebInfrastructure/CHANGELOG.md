# Изменения JoinRpg.Common.WebInfrastructure

Формат — по мотивам [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/).

## Не выпущено

### Ломающие изменения
- Удалены `IDailyJobRepository` и `JobId` из namespace `JoinRpg.Common.WebInfrastructure.DailyJob` (сам namespace исчез). Переход: подключить пакет `JoinRpg.Common.WebInfrastructure.Interfaces` и использовать `IJobRepository` и `JobId` из namespace `JoinRpg.Common.WebInfrastructure.Job`. ([#6020](https://github.com/joinrpg/joinrpg-net/pull/6021))
- Удалён `DbContextRegisterHelper.AddJoinEfCoreDbContext` из namespace `JoinRpg.Common.WebInfrastructure`. Переход: подключить пакет `JoinRpg.Common.EntityFrameworkCore` и вызывать `AddJoinEfCoreDbContext` из namespace `JoinRpg.Common.EntityFrameworkCore` (сигнатура не менялась). ([#6024](https://github.com/joinrpg/joinrpg-net/pull/6041))

### Добавлено
- `AddMidnightJobs()` — единый планировщик ежедневных джоб: находит все джобы, зарегистрированные через `AddJob<T>()` из пакета `JoinRpg.Common.WebInfrastructure.Interfaces`, и запускает каждую at-most-once в сутки (полночь UTC плюс `JobDelay`, настройка выключения — секция `DailyJobs`). Вместе с ним `BackgroundServiceActivity` (ActivitySource `BackgroundJobService`), источник активностей подключается `AddJoinOpenTelemetry` автоматически. ([#6020](https://github.com/joinrpg/joinrpg-net/pull/6021))

### Изменено
- Пакет зависит от `JoinRpg.Common.WebInfrastructure.Interfaces` — контракты джоб (`IJob`, `IJobRepository`, `IJobRunner`, `JobsOptions`, `AddJob`) выделены туда. ([#6020](https://github.com/joinrpg/joinrpg-net/pull/6021))
- Регистрация DbContext (`AddJoinEfCoreDbContext`) переехала в `JoinRpg.Common.EntityFrameworkCore`; зависимости `EntityFrameworkCore.Exceptions.PostgreSQL` и `AspNetCore.HealthChecks.NpgSql` больше не нужны. ([#6024](https://github.com/joinrpg/joinrpg-net/pull/6041))

## 2026.9.533 — 2026-10-29

### Ломающие изменения
- Миграция `JoinRpg.Dal.DataProtection.Migrations.Initial` стала `internal`. Переход: не ссылаться на неё — EF находит миграцию сам. ([#5960](https://github.com/joinrpg/joinrpg-net/pull/5960))

### Добавлено
- Пакет: CHANGELOG.md, лицензия MIT. ([#5948](https://github.com/joinrpg/joinrpg-net/pull/5948))

## 2026.9.438

### Изменено
- Зависимости `Microsoft.EntityFrameworkCore` и `Microsoft.AspNetCore.*.EntityFrameworkCore` — 10.0.14. ([#5868](https://github.com/joinrpg/joinrpg-net/pull/5868), [#5872](https://github.com/joinrpg/joinrpg-net/pull/5872))

## 2026.9.363

### Ломающие изменения
- `AddJoinWebPlatform`: редирект http → https и HSTS (вне Development), кроме `/health*` и `/metrics`. Переход: прокси передаёт `X-Forwarded-Proto`, или `HttpsRedirection:Enabled=false`. ([#5588](https://github.com/joinrpg/joinrpg-net/pull/5588))
- `AddJoinWebPlatform`: сам ставит `UseForwardedHeaders()` в начало конвейера. Переход: убрать свой вызов `app.UseForwardedHeaders()`. ([#5587](https://github.com/joinrpg/joinrpg-net/pull/5587))

### Добавлено
- `AddJoinWebPlatform` над `WebApplicationBuilder` (с Serilog). ([#5580](https://github.com/joinrpg/joinrpg-net/pull/5580))
- `JoinHttpsRedirectionOptions` (секция `HttpsRedirection`). ([#5588](https://github.com/joinrpg/joinrpg-net/pull/5588))
- `UseErrorPageReExecuteAsGet()`: страница ошибки при re-execute запрашивается методом GET. ([#5616](https://github.com/joinrpg/joinrpg-net/pull/5616))
- `ErrorSupportInfo.FromHttpContext()`: данные для техподдержки на странице ошибки. ([#5614](https://github.com/joinrpg/joinrpg-net/pull/5614))
- `AppVersionInfo`: версия, SHA и ссылка на коммит приложения. ([#5610](https://github.com/joinrpg/joinrpg-net/pull/5610), [#5614](https://github.com/joinrpg/joinrpg-net/pull/5614))
- Счётчик `joinrpg.http.requests.private_network` (meter `JoinRpg.WebInfrastructure`): запросы с внутренних адресов после ForwardedHeaders. ([#5587](https://github.com/joinrpg/joinrpg-net/pull/5587), [#5590](https://github.com/joinrpg/joinrpg-net/pull/5590))

### Изменено
- `AddJoinOpenTelemetry`: экспортирует meter'ы `JoinRpg` и `JoinRpg.WebInfrastructure`. ([#5587](https://github.com/joinrpg/joinrpg-net/pull/5587), [#5691](https://github.com/joinrpg/joinrpg-net/pull/5691))
- Зависимости OpenTelemetry — 1.20. ([#5719](https://github.com/joinrpg/joinrpg-net/pull/5719), [#5762](https://github.com/joinrpg/joinrpg-net/pull/5762))

Более ранние версии — [история коммитов](https://github.com/joinrpg/joinrpg-net/commits/master/src/Common/JoinRpg.Common.WebInfrastructure).
