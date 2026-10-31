# Изменения JoinRpg.Common.EntityFrameworkCore

Формат — по мотивам [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/).

## Не выпущено

### Добавлено
- `AddJoinEfCoreDbContext<TContext>()` — регистрация DbContext: PostgreSQL (Npgsql), EntityFramework.Exceptions (бизнес-исключения вместо `DbUpdateException`), лог SQL в Debug в dev-режиме, health check `{имя строки подключения}-db` (Degraded при недоступности). Без строки подключения ничего не регистрирует и возвращает `false`. Перенесён из `JoinRpg.Common.WebInfrastructure` (namespace `JoinRpg.Common.EntityFrameworkCore`). ([#6024](https://github.com/joinrpg/joinrpg-net/pull/6041))

### Изменено
- Новые зависимости: `Npgsql.EntityFrameworkCore.PostgreSQL`, `EntityFrameworkCore.Exceptions.PostgreSQL`, `AspNetCore.HealthChecks.NpgSql`, `Microsoft.Extensions.Diagnostics.HealthChecks`, `Microsoft.Extensions.Hosting.Abstractions`, `Microsoft.Extensions.Configuration.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`. ([#6024](https://github.com/joinrpg/joinrpg-net/pull/6041))

## 2026.9.533 — 2026-10-29

### Добавлено
- Пакет: README.md и CHANGELOG.md, лицензия MIT. ([#5948](https://github.com/joinrpg/joinrpg-net/pull/5948), [#5953](https://github.com/joinrpg/joinrpg-net/pull/5953))

## 2026.9.438

### Изменено
- Зависимость `Microsoft.EntityFrameworkCore` — 10.0.14. ([#5872](https://github.com/joinrpg/joinrpg-net/pull/5872))

## 2026.9.363

Видимых изменений нет.

Более ранние версии — [история коммитов](https://github.com/joinrpg/joinrpg-net/commits/master/src/Common/JoinRpg.Common.EntityFrameworkCore).
