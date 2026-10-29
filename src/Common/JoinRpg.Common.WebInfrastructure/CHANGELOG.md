# Изменения JoinRpg.Common.WebInfrastructure

Формат — по мотивам [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/).

## Не выпущено

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
