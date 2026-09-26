# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## О проекте

Joinrpg.ru — сайт для организации живых ролевых игр (LARP): связывает организаторов с игроками.
Основное приложение: ASP.NET Core 10 + Blazor WebAssembly (острова/islands).

## Документация

Индекс всей документации разработчика — [docs/README.md](docs/README.md). **Перед нетривиальной задачей сначала загляни туда**: это оглавление по архитектуре, гайдам и ADR, оттуда есть ссылки на нужный документ (например [docs/structure.md](docs/structure.md) — куда класть код и соглашения по `.csproj`).

## Команды

### Локальный запуск

```bash
dotnet tool restore                              # Локальные инструменты из .config/dotnet-tools.json (первый запуск)
docker compose up -d                             # PostgreSQL (5432) и SQL Server (1433)
dotnet run --project src/Joinrpg.Dal.Migrate     # Применить миграции БД (первый запуск)
dotnet run --project src/JoinRpg.Portal          # Основной сайт на https://localhost:5001
dotnet run --project src/JoinRpg.IdPortal/JoinRpg.IdPortal  # Портал аккаунтов
```

Первый вошедший пользователь автоматически получает подтверждённый email и роль администратора.
Email в dev-режиме не отправляется — логируется. OAuth требует ключей в `appsettings.json`.

Для тестовых пользователей (`admin@example.com`, `master@example.com`, `player@example.com`,
пароль `Test12345!`) и тестового проекта «Тестовая песочница» в локальной БД есть отдельный
скрипт — см. [docs/local-dev-test-users.md](docs/local-dev-test-users.md). Если их нет в локальной
БД и они нужны для тестирования — запусти `dotnet run --project src/JoinRpg.Tools.SeedTestUsers`.

### Сборка и форматирование

```bash
dotnet build
dotnet format                                             # Применить стиль кода (обязательно перед коммитом)
dotnet format --verify-no-changes --severity warn         # Проверка (CI)
```

### Коммиты

При создании коммита всегда открывать редактор, чтобы пользователь мог исправить сообщение:

```bash
git commit --edit -m "предложенное сообщение коммита"
```

### Ребейз: upstream, а не origin

`origin` — личный форк (`leotsarev/joinrpg-net`), основной репозиторий — `upstream` (`joinrpg/joinrpg-net`).
Туда же идут PR. Поэтому ребейзить и сверяться с базовой веткой всегда через `upstream`:

```bash
git fetch upstream
git rebase upstream/master
```

`origin/master` в форке отстаёт и на «ребейзни» отвечает «уже актуально» — это ложный ответ, не верить ему.

### GitHub: создание PR после завершения issue

**ОБЯЗАТЕЛЬНО**: При работе в контексте GitHub (через claude-code-action) — после коммита кода сразу создавать PR через `gh pr create`. Не предоставлять ссылку «Create PR» — создавать PR самостоятельно. Не ждать отдельной просьбы.

```bash
gh pr create --title "feat: описание" --body "$(cat <<'EOF'
## Что сделано
- ...

Closes #НОМЕР

Generated with [Claude Code](https://claude.ai/code)
EOF
)"
```

### GitHub: всегда подписываться как Claude

**ОБЯЗАТЕЛЬНО**: любое сообщение, отправляемое в GitHub от лица пользователя (комментарий к issue или PR,
описание PR, текст ревью, тело issue), должно быть подписано — из него должно быть явно видно, что писал Claude,
а не человек. Подпись ставится последней строкой:

```
🤖 Generated with [Claude Code](https://claude.ai/code)
```

Это касается всех команд `gh` (`gh pr create`, `gh pr comment`, `gh issue create`, `gh issue comment`,
`gh pr review`) и любых других способов отправки текста в GitHub. Учётная запись GitHub принадлежит человеку,
поэтому без подписи сообщение выглядит написанным им лично — это вводит собеседников в заблуждение.

### Тесты

```bash
dotnet test                                      # Все тесты
dotnet test src/JoinRpg.Domain.Test              # Конкретный тестовый проект
```

## Архитектура по слоям

При создании новых классов или проектов — читать [docs/structure.md](docs/structure.md).

### LINQ-запросы и LinqKit

EF-предикаты и проекции пишутся через `Expression<Func<...>>` + LinqKit.
Подробнее — [docs/linq-queries.md](docs/linq-queries.md).

### Blazor WebAssembly (islands, ADR001)

Небольшие интерактивные виджеты Blazor встраиваются в Razor-страницы.
`JoinRpg.Blazor.Client` — WA-клиент основного портала (в процессе миграции).
`JoinRpg.Common.WebComponentBook.Portal`/`.Client` — каталог компонентов для разработки (аналог Storybook), задеплоен на components.joinrpg.ru.

### Фоновые задачи (ADR002)

Паттерн `MidnightJobBackgroundService<TJob>`. Выполнение at-most-once в сутки (tracking в `JoinRpg.Dal.JobService`). Задачи должны быть идемпотентны.

### Уведомления (ADR003)

`JoinRpg.Services.Notifications` — оркестратор (каналы: email, Telegram, in-app).
`JoinRpg.Dal.Notifications` — хранение очереди уведомлений.

## Инструменты анализа кода

Для понимания кода предпочитай **cwm-roslyn-navigator** (MCP) перед чтением файлов. Roslyn понимает семантику C#: типы, иерархии наследования, реальные использования символов. Используй `find_symbol`, `find_references`, `find_callers`, `find_implementations`, `get_type_hierarchy` и другие инструменты навигатора для навигации по коду. Чтение файлов — только когда нужен полный контекст, которого навигатор не даёт.

Сервер прописан в `.mcp.json` и запускается локальным инструментом из `.config/dotnet-tools.json`,
поэтому на свежем клоне нужен `dotnet tool restore` — без него сервер не поднимется. Решение
(`Joinrpg.slnx`) навигатор находит сам, аргументы не нужны. Новый MCP-сервер подхватывается при
старте сессии: если он только что появился, текущая сессия его не увидит.

Почему это важно на практике: грепом по именам **не видно мёртвых перегрузок** — одноимённый живой
метод прячет мёртвый. Так `ICharacterPropsService.ChangeCharacter<TArgs, TResult>` и
`CharacterOperationContextExtensions.MarkCreatedNow` прожили лишнее время: у второго четыре вызова,
но все они уходят в одноимённое расширение над контекстом проекта. `find_references` такое находит
сразу.

## Фикс багов

При исправлении бага **всегда рассматривать добавление юнит теста**, который воспроизводит баг до фикса и проходит после. Это предотвращает регрессии. Тест добавляется в соответствующий тестовый проект.

## Правила кода

- **Без regex** — по возможности избегать регулярных выражений.
- **Не править `.cs` потоковыми утилитами** (`sed`, `awk`, перезапись через shell-редирект). В рабочем дереве файлы с CRLF, такие правки ставят в изменённых строках LF — получается смешанный перевод строк, и `dotnet format --verify-no-changes` падает в CI. Массовые правки делать редактором/IDE, а не скриптом.
- **Язык UI**: все видимые пользователю строки (названия колонок, кнопки, заголовки, сообщения) — всегда на русском языке.
- **Локализация**: строки видимые пользователю — только в `JoinRpg.Portal` и `JoinRpg.Services.Email`. В остальных проектах — `TODO[Localize]` при нарушении.
- **DI**: Autofac. Модули регистрируются в `Startup.cs`/`Program.cs` через `ConfigureContainer()`.
- **Новые поля в DataModel**: согласовывать изменения с @leotsarev.
- **Версия .NET SDK**: не обновлять вручную в `global.json`. Там прописан `"rollForward": "latestFeature"` — SDK автоматически использует последнюю доступную feature-версию. Dependabot обновляет только NuGet-пакеты, но не SDK, и это нормально.

## Сущности

Инструкцию о создании зависимой сущности проекта можно прочесть в [docs/project-entities.md](docs/project-entities.md)
