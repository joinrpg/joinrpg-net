# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## О проекте

Joinrpg.ru — сайт для организации живых ролевых игр (LARP): связывает организаторов с игроками.
Основное приложение: ASP.NET Core 10 + Blazor WebAssembly (острова/islands).

## Команды

### Локальный запуск

```bash
docker compose up -d                             # PostgreSQL (5432) и SQL Server (1433)
dotnet run --project src/Joinrpg.Dal.Migrate     # Применить миграции БД (первый запуск)
dotnet run --project src/JoinRpg.Portal          # Основной сайт на https://localhost:5001
dotnet run --project src/JoinRpg.IdPortal/JoinRpg.IdPortal  # Портал аккаунтов
```

Первый вошедший пользователь автоматически получает подтверждённый email и роль администратора.
Email в dev-режиме не отправляется — логируется. OAuth требует ключей в `appsettings.json`.

### Сборка и форматирование

```bash
dotnet build
dotnet format                                             # Применить стиль кода (обязательно перед коммитом)
dotnet format --verify-no-changes --severity error        # Проверка (CI)
```

### Коммиты

При создании коммита всегда открывать редактор, чтобы пользователь мог исправить сообщение:

```bash
git commit --edit -m "предложенное сообщение коммита"
```

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

### Тесты

```bash
dotnet test                                      # Все тесты
dotnet test src/JoinRpg.Domain.Test              # Конкретный тестовый проект
```

## Архитектура по слоям

При создании новых классов или проектов — читать [docs/structure.md](docs/structure.md).

### Blazor WebAssembly (islands, ADR001)

Небольшие интерактивные виджеты Blazor встраиваются в Razor-страницы.
`JoinRpg.Blazor.Client` — WA-клиент основного портала (в процессе миграции).
`JoinRpg.Blazor.ComponentBook` — каталог компонентов для разработки (аналог Storybook).

### Фоновые задачи (ADR002)

Паттерн `MidnightJobBackgroundService<TJob>`. Выполнение at-most-once в сутки (tracking в `JoinRpg.Dal.JobService`). Задачи должны быть идемпотентны.

### Уведомления (ADR003)

`JoinRpg.Services.Notifications` — оркестратор (каналы: email, Telegram, in-app).
`JoinRpg.Dal.Notifications` — хранение очереди уведомлений.

## Инструменты анализа кода

Для понимания кода предпочитай **cwm-roslyn-navigator** (MCP) перед чтением файлов. Roslyn понимает семантику C#: типы, иерархии наследования, реальные использования символов. Используй `find_symbol`, `find_references`, `find_callers`, `find_implementations`, `get_type_hierarchy` и другие инструменты навигатора для навигации по коду. Чтение файлов — только когда нужен полный контекст, которого навигатор не даёт.

## Фикс багов

При исправлении бага **всегда рассматривать добавление юнит теста**, который воспроизводит баг до фикса и проходит после. Это предотвращает регрессии. Тест добавляется в соответствующий тестовый проект.

## Правила кода

- **Без regex** — по возможности избегать регулярных выражений.
- **Язык UI**: все видимые пользователю строки (названия колонок, кнопки, заголовки, сообщения) — всегда на русском языке.
- **Локализация**: строки видимые пользователю — только в `JoinRpg.Portal` и `JoinRpg.Services.Email`. В остальных проектах — `TODO[Localize]` при нарушении.
- **DI**: Autofac. Модули регистрируются в `Startup.cs`/`Program.cs` через `ConfigureContainer()`.
- **Новые поля в DataModel**: согласовывать изменения с @leotsarev.
- **Версия .NET SDK**: не обновлять вручную в `global.json`. Там прописан `"rollForward": "latestFeature"` — SDK автоматически использует последнюю доступную feature-версию. Dependabot обновляет только NuGet-пакеты, но не SDK, и это нормально.
