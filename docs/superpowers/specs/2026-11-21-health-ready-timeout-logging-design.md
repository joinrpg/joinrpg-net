# Логирование таймаутов health-проб (client abort)

Дата: 2026-11-21
Статус: утверждёно (после разбора: первоначальная гипотеза «в логах ни слова» оказалась неверной)

## Намерение и зачем

Когда readiness/liveness-проба kubernetes таймаутится **на стороне клиента** (kubelet обрывает
соединение раньше, чем чеки завершились), в логах приложения ничего не появляется, хотя это
начало инцидента. При разборе — только 503/timeout в событиях k8s и молчание в логе.

## Что выяснилось при разборе (важно)

Есть два разных сценария таймаута:

1. Чек не уложился в свой `Timeout` (`DefaultHealthCheckTimeout` = 5 с), клиент дождался ответа —
   фреймворк (.NET 10, `Microsoft.AspNetCore.Diagnostics.HealthChecks`) **уже пишет на Error**:
   «Health check {name} with status … / threw an unhandled exception …» с исключением.
   Дублировать это своим логом не нужно.
2. Проба оборвала соединение раньше завершения чеков — `HealthCheckMiddleware` передаёт
   `httpContext.RequestAborted` в `CheckHealthService.CheckHealthAsync`, отмена распространяется
   наружу, отчёт не собирается, per-entry логов фреймворка не существует. Запрос попадает в наш
   `IgnoreAbortedRequest`, который гасит `OperationCanceledException` и пишет **Debug** —
   отфильтрован минимальным уровнем Information. Это и есть «не вижу ничего в логе».

## Цели / не-цели

- Цель: сценарий (2) даёт заметную запись в логе: Warning с путём endpoint'а.
- Не-цели: не дублировать фреймворковый лог чеков (сценарий 1); не менять ответы endpoint'ов;
  не трогать метрики.

## Подход

Изменить `IgnoreAbortedRequest` в `HealthCheckExtensions.cs`: уровень Debug → Warning,
в сообщение добавить путь запроса (`context.Request.Path`) и пояснение, что пробы таймаутятся
на стороне клиента и ответ не отправлен. Логика гашения отмены (никаких 500) не меняется.

Альтернативы, отклонённые:

- Свой лог всех Unhealthy-чеков поверх фреймворка — фреймворк уже пишет Error с именем чека
  и исключением; получили бы дубли.
- Обёртка над `ResponseWriter` (первоначальный план) — лечит несуществующую проблему:
  сценарий 1 уже залогирован, сценарий 2 до ResponseWriter не доходит.

## Детали

- Файл: `src/Common/WebInfrastructure/JoinRpg.Common.WebInfrastructure/HealthCheckExtensions.cs`
  (метод `IgnoreAbortedRequest`).
- Сообщение на русском, с structured-параметром пути.

## Тесты

`HealthCheckAbortedRequestLoggingTest` в `JoinRpg.Common.WebInfrastructure.Test`:

- висящий чек (ждёт `CancellationToken`) + `RequestAborted` уже отменён → делегат endpoint'а
  не бросает исключение, в логе Warning с путём `/health/ready`;
- обычный запрос без отмены → Warning нет.

Тест вызывает `RequestDelegate` endpoint'я напрямую с `DefaultHttpContext` (TestServer
клиентский abort семантику реального Kestrel не воспроизводит).

## Ограничения

- Публичный API не меняется. Пакет `JoinRpg.Common.WebInfrastructure` — запись в CHANGELOG
  не требуется (внутреннее поведение, не API).
