# Структура проектов и правила размещения кода

Этот документ описывает, куда добавлять новый код. Читать при создании новых классов или проектов.

### Кросс-слойные интерфейсы (`JoinRpg.Interfaces`)

`ICurrentUserAccessor` — текущий пользователь, `INotificationService` — очередь уведомлений, `IDailyJob` — фоновые задачи.

## Правила локализации

- `JoinRpg.Portal` и `JoinRpg.Services.Email` — могут содержать видимые пользователю строки.
- Все остальные проекты **не должны** содержать видимых пользователю строк и должны быть нейтральны к локализации. Использовать `TODO[Localize]` при нарушении этого правила.

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
- `JoinRpg.DataModel` — EF-сущности (схема БД). Без логики. Логику — в `JoinRpg.PrimitiveTypes` или в `JoinRpg.Domain` как методы-расширения. Можно реализовать `IValidatableObject` для проверки перед сохранением. Ограничения БД всегда предпочтительнее. 
- `JoinRpg.Data.Interfaces` — интерфейсы репозиториев (чтение). Использовать максимально специфичные методы, чтобы избежать lazy load. Избегать отдачи объектов DataModel
- `JoinRpg.Data.Write.Interfaces` — `IUnitOfWork` для записи. Использовать только из сервисного слоя
- `JoinRpg.Dal.Impl` — реализация репозиториев (EF + MS SQL). Не ссылаться снаружи
- `JoinRpg.Dal.Notifications` — DbContext и репозиторий для хранения уведомлений
- `JoinRpg.Dal.JobService` — DbContext для отслеживания выполнения ежедневных задач
- `Joinrpg.Dal.Migrate` — инструмент миграций БД (точка входа)

### Core
- `JoinRpg.Interfaces` — кросс-слойные интерфейсы (пользователь, уведомления, задачи)
- `JoinRpg.Domain` — доменная логика (методы-расширения над DataModel). Устаревший проект. Вместо использования сущностей БД с методами расширения в доменной логике, надо использовать доменные объекты в `JoinRpg.PrimitiveTypes`
- `Joinrpg.Web.Identity` — реализация ASP.NET Core Identity (`MyUserStore`, `CurrentUserAccessor`)
- `JoinRpg.BlobStorage` — абстракция файлового хранилища (AWS S3)

### Services
- `JoinRpg.Services.Interfaces` — контракты сервисов. 1 метод = 1 действие пользователя
- `JoinRpg.Services.Impl` — реализация бизнес-логики. Проверять права самостоятельно, не доверять входным данным. Инкапсулировать операции чтения (кроме выборки по первичному ключу) внутри репозиториев
- `JoinRpg.Services.Email` — email-сервис (часть функций мигрирует в Notifications)
- `JoinRpg.Services.Export` — экспорт в .xlsx (ClosedXML). Не знает ничего о специфике JoinRpg — универсальный компонент.
- `JoinRpg.Services.Notifications` — оркестровка многоканальных уведомлений
- `JoinRpg.Integrations.KogdaIgra` — бизнес-логика синхронизации с KogdaIgra
- `PscbApi` — HTTP-клиент к платёжной системе ПСБ (Промсвязьбанк)

### Portal (основной сайт)
- `JoinRpg.Portal` — основное ASP.NET Core приложение. Только UI, хранить тут как можно меньше кода. В идеале метод контроллера просто вызывает соот. ViewService внутри JoinRpg.WebPortal.Managers и отдает результат в View или редиректит в случае успеха. 
- `JoinRpg.CommonUI.Models` — общие перечисления для UI и email
- `JoinRpg.WebPortal.Models` — legacy MVC ViewModels
- `JoinRpg.WebPortal.Managers` — подготовка ViewModels для веб-портала

### WebPortalShared (разделяемые UI-компоненты)
- `JoinRpg.WebComponents` — базовые Razor-компоненты. Для каждого компонента должны быть тесты на bUnit.
- `JoinRpg.Web.ProjectCommon` — общий UI для всех страниц, относящимся к проектам
- `JoinRpg.Web.Games` — UI страниц описания проекта, настроек проекта, etc
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

Не используем Moq/NSubstitute. Предпочитаем классические юнит-тесты мокистким.
