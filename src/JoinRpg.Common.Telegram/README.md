# JoinRpg.Common.Telegram

Интеграция с Telegram Bot API: отправка уведомлений и валидация Telegram Login Widget.
Используется библиотека [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot).

## Настройки

Настройки биндятся из секции `Telegram` в `appsettings.json` (класс `TelegramLoginOptions`),
подключение — через `services.AddJoinTelegram()` (`Registration.cs`).

```json
"Telegram": {
  "BotName": "",
  "BotId": "",
  "BotSecret": "",
  "AllowedTimeOffset": "00:00:30",
  "Proxy": {
    "Address": "socks5://proxy.example.com:1080",
    "Username": "",
    "Password": ""
  }
}
```

| Параметр | Обязателен | Описание |
|---|---|---|
| `BotName` | нет | Имя бота (`@username` без `@`). Используется в Telegram Login Widget. Пока пусто — Telegram-интеграция считается выключенной (`Enabled == false`), уведомления шлёт заглушка `StubTelegramNotificationService`, а не реальный бот. |
| `BotId` | да, если `BotName` задан | Numeric id бота, первая часть токена (`{BotId}:{BotSecret}`). |
| `BotSecret` | да, если `BotName` задан | Секретная часть токена бота, выданная BotFather. |
| `AllowedTimeOffset` | нет (по умолчанию 30 секунд) | Насколько старым может быть `auth_date` в ответе Login Widget, чтобы попытка входа считалась валидной. |
| `Proxy` | нет | Настройки proxy для исходящих запросов к Telegram Bot API (см. ниже). Если не задано — используется прямое подключение. |

### Proxy

Если Telegram Bot API недоступен напрямую (например, из-за блокировок), можно направить весь трафик бота
через HTTP или SOCKS5 proxy — секция `Telegram:Proxy` (класс `TelegramProxyOptions`).

| Параметр | Обязателен | Описание |
|---|---|---|
| `Address` | да (если секция `Proxy` задана) | Адрес proxy в виде URI. Схема определяет тип proxy: `http://host:port` — HTTP-proxy, `socks5://host:port` (или `socks4://`) — SOCKS-proxy. |
| `Username` | нет | Логин для аутентификации на proxy. |
| `Password` | нет | Пароль для аутентификации на proxy (используется только если задан `Username`). |

Реализация — `TelegramHttpClientFactory.Create(TelegramProxyOptions?)`: строит `HttpClient` с `WebProxy`,
который передаётся в конструктор `TelegramBotClient`. Если `Proxy` не задан, `TelegramBotClient` создаётся
с `HttpClient` по умолчанию (`httpClient: null`) — прямое подключение, как раньше.

## Health check

`HealthCheckTelegram` (регистрируется как `"Telegram client"`) вызывает `GetMyUserName` через
`ITelegramNotificationService`: `Degraded`, если Telegram выключен (`BotName` пуст), `Healthy` — если бот
отвечает. Проверка идёт через тот же `TelegramBotClient`, поэтому неверно настроенный `Proxy` тоже приведёт
к ошибке в этом health check.
