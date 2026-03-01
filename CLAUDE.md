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

### Тесты

```bash
dotnet test                                      # Все тесты
dotnet test src/JoinRpg.Domain.Test              # Конкретный тестовый проект
```

## Архитектура по слоям

### Правила расположения кода

| Слой | Проект | Что сюда |
|------|--------|----------|
| Примитивы | `JoinRpg.PrimitiveTypes` | Доменные типы и независимые от БД |
| Хелперы | `JoinRpg.Helpers` | Вспомогательные методы без зависимостей от проекта |
| Сущности БД | `JoinRpg.DataModel` | EF-классы без логики. Логику — в `JoinRpg.PrimitiveTypes` как или в `JoinRpg.Domain` как методы-расширения. |
| Доменная логика (устаревший проект) | `JoinRpg.Domain` | Методы-расширения над DataModel. Вместо использования в доменной логике сущностей БД  с методами расширения, надо использовать доменные объекты в `JoinRpg.PrimitiveTypes` |
| Репозитории (чтение) | `JoinRpg.Data.Interfaces` | Интерфейсы `IXxxRepository`. Использовать максимально специфичные методы, чтобы избежать lazy load. Избегать отдачи объектов DataModel |
| Репозитории (запись) | `JoinRpg.Data.Write.Interfaces` | `IUnitOfWork`. Только из сервисов |
| DAL (реализация) | `JoinRpg.Dal.Impl` | Реализация репозиториев. Не ссылаться снаружи |
| Сервисы (контракты) | `JoinRpg.Services.Interfaces` | 1 метод = 1 действие пользователя = 1 транзакция |
| Сервисы (реализация) | `JoinRpg.Services.Impl` | Бизнес-логика. Проверять права самостоятельно, не доверять входным данным |
| UI | `JoinRpg.Portal` | Только UI: читать из репозитория → трансформировать в ViewModel → вернуть View. Или: валидировать → вызвать сервис → редирект |
| UI ViewModels | `JoinRpg.WebPortal.Models` | Legacy MVC ViewModels |
| UI Managers | `JoinRpg.WebPortal.Managers` | UI-специфичная логика подготовки ViewModels |
| Shared UI models | `JoinRpg.CommonUI.Models` | Перечисления, общие между UI и email |
| Blazor DTOs | `JoinRpg.Web.ProjectCommon` | Лёгкие DTO и интерфейсы для Blazor-компонентов |

### Кросс-слойные интерфейсы (`JoinRpg.Interfaces`)

`ICurrentUserAccessor` — текущий пользователь, `INotificationService` — очередь уведомлений, `IDailyJob` — фоновые задачи.

### Blazor WebAssembly (islands, ADR001)

Небольшие интерактивные виджеты Blazor встраиваются в Razor-страницы.
`JoinRpg.Blazor.Client` — WA-клиент основного портала (в процессе миграции).
`JoinRpg.Blazor.ComponentBook` — каталог компонентов для разработки (аналог Storybook).

### Фоновые задачи (ADR002)

Паттерн `MidnightJobBackgroundService<TJob>`. Выполнение at-most-once в сутки (tracking в `JoinRpg.Dal.JobService`). Задачи должны быть идемпотентны.

### Уведомления (ADR003)

`JoinRpg.Services.Notifications` — оркестратор (каналы: email, Telegram, in-app).
`JoinRpg.Dal.Notifications` — хранение очереди уведомлений.

## Полный список проектов

### Common
- `JoinRpg.Helpers` — утилиты без зависимостей от проекта
- `JoinRpg.Markdown` — обработка Markdown (Markdig)
- `JoinRpg.PrimitiveTypes` — доменные типы-значения: идентификаторы и Value Objects
- `JoinRpg.Common.WebInfrastructure` — веб-инфраструктура: логирование (Serilog), кеш, DataProtection, фоновые задачи
- `JoinRpg.Common.Telegram` — интеграция с Telegram Bot
- `JoinRpg.Common.BastiliaRatingClient` — HTTP-клиент к сайту клуба #Бастилия
- `JoinRpg.Common.KogdaIgraClient` — HTTP-клиент к сервису KogdaIgra (сырые вызовы API)

### DAL
- `JoinRpg.DataModel` — EF-сущности (схема БД)
- `JoinRpg.Data.Interfaces` — интерфейсы репозиториев (чтение)
- `JoinRpg.Data.Write.Interfaces` — `IUnitOfWork` для записи
- `JoinRpg.Dal.Impl` — реализация репозиториев (EF Core + PostgreSQL)
- `JoinRpg.Dal.Notifications` — DbContext и репозиторий для хранения уведомлений
- `JoinRpg.Dal.JobService` — DbContext для отслеживания выполнения ежедневных задач
- `Joinrpg.Dal.Migrate` — инструмент миграций БД (точка входа)

### Core
- `JoinRpg.Interfaces` — кросс-слойные интерфейсы (пользователь, уведомления, задачи)
- `JoinRpg.Domain` — доменная логика (методы-расширения над DataModel)
- `Joinrpg.Web.Identity` — реализация ASP.NET Core Identity (`MyUserStore`, `CurrentUserAccessor`)
- `JoinRpg.BlobStorage` — абстракция файлового хранилища (AWS S3)

### Services
- `JoinRpg.Services.Interfaces` — контракты сервисов
- `JoinRpg.Services.Impl` — реализация бизнес-логики
- `JoinRpg.Services.Email` — email-сервис (часть функций мигрирует в Notifications)
- `JoinRpg.Services.Export` — экспорт в .xlsx (ClosedXML)
- `JoinRpg.Services.Notifications` — оркестровка многоканальных уведомлений
- `JoinRpg.Integrations.KogdaIgra` — бизнес-логика синхронизации с KogdaIgra
- `PscbApi` — HTTP-клиент к платёжной системе ПСБ (Промсвязьбанк)

### Portal (основной сайт)
- `JoinRpg.Portal` — основное ASP.NET Core приложение
- `JoinRpg.CommonUI.Models` — общие перечисления для UI и email
- `JoinRpg.WebPortal.Models` — legacy MVC ViewModels
- `JoinRpg.WebPortal.Managers` — подготовка ViewModels для веб-портала

### WebPortalShared (разделяемые UI-компоненты)
- `JoinRpg.WebComponents` — базовые Razor-компоненты. Для каждого компонента должны быть тесты на bUnit, дописывай их когда добавляешь компонент.
- `JoinRpg.Web.ProjectCommon` — лёгкие Blazor-DTO и интерфейсы клиентов
- `JoinRpg.Web.Games` — UI для игр
- `JoinRpg.Web.Claims` — UI для заявок игроков
- `JoinRpg.Web.Plots` — UI для вводных (сюжетные элементы/квесты для игроков)
- `JoinRpg.Web.CharacterGroups` — UI для групп персонажей
- `JoinRpg.Web.CheckIn` — UI для регистрации игроков на игре
- `JoinRpg.Web.ProjectMasterTools` — инструменты мастера игры
- `JoinRpg.Web.AdminTools` — инструменты администратора платформы
- `JoinRpg.XGameApi.Contract` — DTO-контракты API для интеграции с внешними игровыми системами
- `JoinRpg.Blazor.Client` — Blazor WebAssembly клиент (в процессе миграции страниц)
- `JoinRpg.Blazor.ComponentBook` — каталог компонентов для разработки (аналог Storybook)

### IdPortal (портал аккаунтов)
- `JoinRpg.IdPortal` — отдельное ASP.NET Core приложение для управления аккаунтами
- `JoinRpg.IdPortal.Client` — Blazor WebAssembly клиент IdPortal

### Tests
- `JoinRpg.TestHelpers` — общие тестовые утилиты
- `JoinRpg.DataModel.Mocks` — моки DataModel
- Тестовые проекты для каждого слоя: Domain, Services.Impl, Portal, IdPortal, Managers, Models, PrimitiveTypes, Markdown, Helpers, Notifications, KogdaIgra, CommonUI.Models

## Правила кода

- **Без regex** — по возможности избегать регулярных выражений.
- **Локализация**: строки видимые пользователю — только в `JoinRpg.Portal` и `JoinRpg.Services.Email`. В остальных проектах — `TODO[Localize]` при нарушении.
- **DI**: Autofac. Модули регистрируются в `Startup.cs`/`Program.cs` через `ConfigureContainer()`.
- **Новые поля в DataModel**: согласовывать изменения с @leotsarev.
