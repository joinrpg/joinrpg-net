# Создание Telegram-канала с страницы /admin/advertisement — дизайн

Дата: 2026-11-26
Статус: согласовано в обсуждении, builder-check пройден

## Зачем

Страница `/admin/advertisement` (`AdvertisementDashboard.cshtml`) сейчас read-only:
гриды каналов и расписаний, CRUD отложен (ADR010 §8). Новый Telegram-канал для рекламы
горячих вакансий добавляется только руками в БД или миграцией. Нужна кнопка «Добавить канал»
с диалогом: админ выбирает из выпадающего списка чат/канал, куда добавлен бот, и подтверждает
создание канала в системе.

## Цели

- Кнопка «Добавить канал» на `/admin/advertisement`, открывающая диалог.
- В диалоге — выпадающий список Telegram чатов/каналов, доступных боту, для которых
  ещё нет канала в `AdvertisementChannels`.
- После подтверждения — создание канала (запись в `AdvertisementChannels`), новый канал
  виден в гриде каналов.

## Не-цели

- Редактирование/удаление/деактивация каналов, CRUD расписаний.
- Привязка канала к проекту (`ProjectId`): создаются только общие каналы (`ProjectId = null`),
  как существующие.
- Полный учёт чатов бота за всё время (требует накопительной таблицы и джобы — сознательно
  отклонено, см. «Подходы»).
- Мультибот (ADR010 §5) — работаем только с дефолтным ботом.
- Обработка входящих апдейтов в реальном времени (webhook/long-polling).

## Ограничения и факты о текущем коде

- Telegram Bot API **не имеет** метода «перечислить мои чаты». Источник списка —
  входящие апдейты `my_chat_member`, которые Telegram хранит ~24 часа. Бот сейчас
  апдейты не обрабатывает (`JoinRpg.Common.Telegram` — только исходящие отправки,
  `TelegramNotificationServiceImpl.cs`).
- `TelegramBotClient` (библиотека Telegram.Bot) уже зарегистрирован в DI как singleton
  (`JoinRpg.Common.Telegram/Registration.cs:29-34`). Если `BotName` не настроен
  (dev-режим), Telegram выключен и вместо сервиса стоит стаб.
- Канал = `AdvertisementChannelEntity` (`AdvertisementChannels`): `Name`, `ProjectId?`,
  `Kind=Telegram`, `SettingsJson` (`TelegramChannelSettings` с `ChatId`), `IsActive`.
  Доменный тип — `PostingChannelInfo` (`JoinRpg.DomainTypes/Posting`).
- `IPostingChannelRepository` — только чтение. По ADR010 §3 write-операции должны идти
  через `IUnitOfWork.GetXxx()`.
- Страница — Razor Page с Static-компонентами; интерактивные острова в админке уже есть
  (пример: `AdminProjectPanel` на `Areas/Admin/Views/ProjectList/Index.cshtml`).
- Chat id не секрет (ADR010 §5) — показ списка в админке допустим. Страница под
  `[AdminAuthorize]`.
- UI-строки — на русском (правило репозитория).

## Подходы

**A. Blazor-остров с диалогом, перезагрузка после создания — ВЫБРАН.**
Кнопка и диалог — интерактивный WASM-остров на существующей Static-странице. После успешного
создания страница перезагружается, Static-грид показывает новый канал. Минимум кода,
все паттерны уже есть в проекте.

**B. Полностью интерактивный грид каналов.** Перевод `AdvertisementChannelsGrid` на WASM,
добавление строки без перезагрузки. Отклонён: заметно больше работы ради одной кнопки.

**C. Серверная форма на Razor Page без Blazor.** Отклонён: против ADR001 (движение к
Blazor-островам), и `GetUpdates` вызывался бы при каждом заходе на страницу, а не по требованию.

**Источник списка чатов — разовый `GetUpdates` при открытии диалога (выбрано пользователем).**
Альтернативы: накопительная таблица + джоба (полный список, но новая таблица/миграция/джоба) —
отклонено как избыточное для текущей потребности; ручной ввод `@username` с проверкой через
`GetChat` — отклонено, не соответствует запрошенному UX «выпадашки».

Следствие выбора: в списке видны только чаты, куда бота добавили (или изменился его статус)
за последние ~24 часа. Рабочий сценарий: админ добавляет бота в канал → открывает диалог →
чат в списке. Это ограничение фиксируем в подсказке в диалоге.

## Дизайн

### UI (остров)

- Новый компонент `AdvertisementChannelCreatePanel` в
  `src/Web/JoinRpg.Web.AdminTools/Advertisement/` — кнопка «Добавить канал» + `JoinFormDialog`
  (существующий, `JoinRpg.Common.WebComponents`).
- На `AdvertisementDashboard.cshtml` рядом с гридом каналов добавляется остров
  (`render-mode="WebAssemblyPrerendered"`).
- Диалог:
  - При открытии — `GET /webapi/advertisementchannel/availablechats`, список в `<select>`.
    Отображение: название чата + chat id.
  - Поле «Название» — текстовое, предзаполнено title выбранного чата, редактируемо,
    обязательное.
  - Если список пуст — сообщение: «Нет доступных чатов. Добавьте бота @<BotName> в канал
    и откройте диалог заново (Telegram хранит события ~24 часа)».
  - Если Telegram не настроен (dev) — сообщение «Telegram-бот не настроен» вместо списка.
- После успешного `POST` — перезагрузка страницы (`NavigationManager.NavigateTo` с
  `forceLoad: true`).
- Ошибки API (Telegram недоступен, chat id уже занят) — сообщение в диалоге, без перезагрузки.

### Клиентская связка (по существующему паттерну)

- `IAdvertisementChannelClient` в `JoinRpg.Web.AdminTools`:
  `GetAvailableChatsAsync()`, `CreateChannelAsync(request)`.
- Реализация `AdvertisementChannelClient` в `JoinRpg.Blazor.Client/ApiClients/`,
  регистрация в `HttpClientRegistration.cs`; POST — с `CsrfTokenProvider`.
- DTO (в `JoinRpg.Web.AdminTools` или общем контрактном проекте по соседним примерам):
  `AvailableChatsResponse(bool Enabled, string? BotName, IReadOnlyCollection<AvailableChatDto> Chats)`,
  `AvailableChatDto(long ChatId, string Title)`, `CreateChannelRequest(long ChatId, string Name)`.
  Флаги `Enabled`/`BotName` нужны, чтобы диалог различал состояния «Telegram не настроен»
  (dev) и «нет доступных чатов» — пустой список их не различает, а `BotName` для подсказки
  живёт только на сервере (`TelegramLoginOptions`). *(решение исполнителя по builder-check)*

### WebApi

- `WebApi/AdvertisementChannelController` в `JoinRpg.Portal/Controllers/WebApi/`,
  `[Route("/webapi/advertisementchannel/[action]")]`, `[AdminAuthorize]`, antiforgery на POST.
- `GET AvailableChats`:
  1. Сервис Telegram возвращает чаты бота (см. ниже).
  2. Контроллер читает существующие каналы через `IPostingChannelRepository.GetAllChannels()`,
     извлекает `ChatId` из `TelegramChannelSettings` (десериализация уже есть в
     `AdvertisementEntityMapper`) и исключает их из списка. Исключаются **все** каналы,
     включая деактивированные (`IsActive = false`): иначе один chat id можно было бы
     завести дважды. Повторная активация деактивированного канала — по-прежнему руками
     в БД (вне скоупа). *(решение исполнителя по builder-check)*
- `POST Create`:
  - Валидация: имя не пустое; chat id присутствует в списке доступных (тот же запрос к
    Telegram — защита от подделки запроса) и ещё не занят существующим каналом.
  - Создание записи: `Kind = Telegram`, `SettingsJson` из `TelegramChannelSettings`,
    `IsActive = true`, `ProjectId = null`.
  - Ответ: 200 → клиент перезагружает страницу; 409/400 с сообщением → показ в диалоге.

### Telegram-часть

- Новый сервис в `JoinRpg.Common.Telegram`, например `ITelegramBotChatsService` /
  `TelegramBotChatsService`, метод `GetBotChatsAsync() → IReadOnlyCollection<TelegramBotChat>`
  (`TelegramBotChat(long ChatId, string Title)`).
- Реализация: `client.GetUpdates()` с `allowed_updates: ["my_chat_member"]`, **без offset** —
  апдейты не подтверждаются и остаются доступными для повторных вызовов и других читателей.
  Фильтр `allowed_updates` серверный: лимит Telegram в 100 апдейтов на вызов после фильтра
  касается только событий `my_chat_member`, которых за 24 часа набирается мало. Без фильтра
  очередь забита посторонними сообщениями бота (`/start` и т.п.) и событие добавления бота
  в канал могло бы не попасть в первую сотню. Подтверждать апдейты (`offset`) сознательно
  не решаемся: возможны внешние читатели очереди этого бота вне репозитория. Зафиксировать
  комментарием: если появится webhook или другой потребитель апдейтов, это место сломается.
  Теоретический остаточный риск (>100 событий `my_chat_member` за сутки) принят.
- Из апдейтов берутся `my_chat_member` (chat type: channel, group, supergroup), учитывается
  **последний** статус бота на чат: `member`/`administrator` — чат доступен, `left`/`kicked` —
  исключить. Название — `chat.title` из апдейта.
- Дедупликация по chat id.
- Если Telegram выключен — стаб возвращает «не настроен» (по аналогии со
  `StubTelegramNotificationService`), диалог показывает соответствующее сообщение.

### Репозиторий

- В `IPostingChannelRepository` (или новый write-репозиторий через `IUnitOfWork` — по ADR010 §3,
  решить при реализации по месту существующих write-репозиториев) добавить
  `CreateChannel(PostingChannelInfo)` / эквивалент. Маппинг — переиспользовать
  `AdvertisementEntityMapper`.

### Обработка ошибок

- Telegram API недоступен/таймаут — диалог показывает «Не удалось получить список чатов,
  попробуйте позже».
- Гонка «chat id занят» между открытием диалога и подтверждением — 409, сообщение в диалоге.

## Тесты

- `JoinRpg.Common.Telegram.Test`: юнит-тесты фильтрации апдейтов (последний статус на чат,
  kicked/left исключаются, дедупликация, не-my_chat_member апдейты игнорируются).
- Тест создания канала через репозиторий/маппер (по образцу существующих тестов DAL).
- Баг-регрессионные тесты не требуются (не фикс бага); покрытие — по правилам проекта.

## Открытые мелочи (решения исполнителя)

- Точное имя сервиса/методов и место DTO — по соседним примерам в коде.
- Форма write-доступа (расширение существующего репозитория vs `IUnitOfWork`) — по ADR010 §3
  и существующим write-репозиториям.
