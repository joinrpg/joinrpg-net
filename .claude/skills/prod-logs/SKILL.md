---
name: prod-logs
description: Читать и фильтровать логи продакшна и dev-окружения joinrpg.ru из сервиса Логи Selectel (k8s-логи Portal/IdPortal/ComponentBook/миграций) через aws CLI. Используй, когда нужно разобрать ошибку на проде, посмотреть последние запросы, найти конкретный трейс/RequestId или действия пользователя.
---

# Логи продакшна (Selectel Logs)

stdout подов собирает fluent-bit (DaemonSet в namespace `infra`, чарт — `platform/fluent-bit` в
[joinrpg/joinrpg-helms](https://github.com/joinrpg/joinrpg-helms/tree/main/platform/fluent-bit)) и шлёт в сервис
[Логи Selectel](https://docs.selectel.ru/en/logs/), пул ru-6. API сервиса совместим с AWS CloudWatch Logs,
читаем его aws CLI. Хранение — 30 дней. Пишутся с 2026-10-15, с переезда кластера в Selectel.

## Доступ

aws CLI с профилем `selectel-logs` (настраивает пользователь, один раз):

- ключ — «S3-ключ» сервисного пользователя Selectel с ролью `reader` или `logs.viewer`, проект JoinRpgCloud.
  Другого способа авторизации API логов нет: IAM-токен, как в скилле **selectel-readonly**, он не принимает.
  Ключ fluent-bit (`logs.writer`) читать не может;
- `aws configure --profile selectel-logs` (регион `ru-6`), затем
  `aws configure set endpoint_url https://ru-6.logs.selcloud.ru --profile selectel-logs`.

Ключи — в `~/.aws/credentials` пользователя, не в репозитории и не в чате. Проверка:
`aws logs describe-log-groups --profile selectel-logs --query "logGroups[].logGroupName"`.

Писать `--profile selectel-logs` в команде буквально, не через переменную или splat (`@P`): правило
разрешения в личных настройках (`aws logs * --profile selectel-logs*`) сверяется с текстом команды.

## Группы и потоки

- Группа на namespace: `k8s/joinrpg-prod`, `k8s/joinrpg-dev` — так отличаются прод и дев.
  Ещё есть `k8s/kogda-prod`, `k8s/kogda-dev`, `k8s/larp-dictionary`, а также логи сервисов Selectel:
  `s/dbaas/<кластер>` (PostgreSQL), `s/lbaas/<балансировщик>`.
- Поток на контейнер: `joinrpg-portal`, `joinrpg-idportal`, `joinrpg-componentbook`, `joinrpg-dal-migrate`.
- Общий поток `_all` — все контейнеры namespace. **Каждая запись лежит дважды**: в потоке контейнера и в `_all`.
  Поэтому всегда указывать `--log-stream-names`: `_all` для всего namespace или имя контейнера для одного
  приложения. Без него выдача удваивается.
- Имени пода в имени потока нет (раньше было и упёрлось в лимит Selectel — 100 потоков на группу).
  Под — в поле `kubernetes.pod_name` записи.

## Базовая команда

```powershell
$now  = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
$from = $now - 3600000   # час назад
aws logs filter-log-events --profile selectel-logs --log-group-name k8s/joinrpg-prod --log-stream-names _all `
  --start-time $from [--end-time $to] [--filter-pattern '<термы>'] --max-items 1000 --output json > logs.json
```

- `--start-time`/`--end-time` — миллисекунды Unix epoch (UTC). Время из лога в ms:
  `[DateTimeOffset]::Parse('2026-10-19T17:30:00Z').ToUnixTimeMilliseconds()`.
- **Без `--start-time` выдача начинается с самого старого события в потоке**, а не с последних.
- События идут **от старых к новым**, и `--max-items` отрезает **новые**. Если вернулось ровно `--max-items`
  записей и есть `NextToken` — выдача обрезана: сузить окно или поднять лимит. Чтобы увидеть последние
  события, сужать `--start-time`, а не уменьшать лимит.
- CLI сам листает страницы до `--max-items`; 1000 записей — около 3–5 секунд.
- Выдачу сохранять в файл и разбирать им, а не читать в контекст целиком: одна запись — около 2 КБ.

## Фильтры

Работают только **текстовые** filter-pattern (проверено 2026-10-19):

| Шаблон | Что делает |
|---|---|
| `ERROR` | терм — подстрока в записи |
| `GetForClaim 1611` | все термы сразу (AND) |
| `?GetForClaim ?GetCharactersByType` | любой из термов (OR) |
| `GetForClaim -1611` | исключить терм |
| `"atana@bastilia.ru"` | точная фраза |

**JSON-фильтры (`{ $.Level = "Error" }`, `{ $.StatusCode >= 500 }`) не поддерживаются** — молча возвращают
пустую выдачу, даже на условии, которому заведомо есть совпадения. Пустой результат с `{ $... }` не значит
«ничего не было». Фразы с экранированными кавычками (`"\"YcLevel\":\"ERROR\""`) тоже не матчатся.

Поэтому фильтровать грубо термом на сервере, а точно — на клиенте, разобрав `message`:

```powershell
$j = Get-Content logs.json -Raw | ConvertFrom-Json
$j.events | ForEach-Object { $_.message | ConvertFrom-Json } |
  Where-Object { $_.kubernetes.container_name -eq 'joinrpg-portal' -and $_.StatusCode -ge 500 } |
  Select-Object '@timestamp', Level, StatusCode, RequestPath, TraceId, message
```

Удобные термы:

- уровень — `ERROR` / `WARN`: на практике матчат записи этих уровней (поле `YcLevel`), но терм может
  попасть и в текст сообщения — уровень перепроверять полем;
- `TraceId`, `RequestId`, email пользователя, `SourceContext`, имя действия контроллера — уникальны, как термы работают точно;
- `ProjectId` числом — шумный терм (то же число встречается в других id); лучше сузить на клиенте.

## Структура записи

`events[].message` — **JSON строкой** (запись fluent-bit), его надо разбирать ещё раз. Поля Serilog лежат
в корне этой записи (`Merge_Log On`), рядом — метаданные `kubernetes`:

```jsonc
{
  "@timestamp": "2026-10-19T18:23:47.062Z",
  "Level": "Information",          // Serilog: Verbose/Debug/Information/Warning/Error/Fatal
  "YcLevel": "INFO",               // то же для облака: INFO/WARN/ERROR — по нему удобнее фильтровать
  "message": "HTTP \"GET\" \"/webapi/...\" responded 200 in 45.4 ms",
  "AppName": "JoinRpg.Portal",
  "SourceContext": "Serilog.AspNetCore.RequestLoggingMiddleware",
  "TraceId": "...", "SpanId": "...", "RequestId": "0HNPDQID3O373:00000026",
  "ActionName": "JoinRpg.Portal.Controllers....GetCharactersByType (JoinRpg.Portal)",
  "RequestMethod": "GET", "RequestPath": "/webapi/characters/GetCharactersByType", "StatusCode": 200, "Elapsed": 45.4,
  "ProjectId": 1611,
  "LoggedUser": "user@example.com",
  "MachineName": "joinrpg-portal-55548f6c85-68dhc",
  "exceptions": [{ "ClassName": "...", "Message": "...", "StackTraceString": "...", "Depth": 0 }],
  "fields": { "http.request.method": "GET", "url.path": "...", "user_agent.original": "...", "Referer": "...",
              "lazyLoadTableName": "[dbo].[Claims]" },
  "kubernetes": { "pod_name": "...", "container_name": "joinrpg-portal", "namespace_name": "joinrpg-prod",
                  "host": "joinrpg-node-x3ic0", "labels": { "pod-template-hash": "55548f6c85" },
                  "container_hash": "cr.selcloud.ru/joinrpg/joinrpg-portal@sha256:..." },
  "stream": "stdout", "time": "..."
}
```

`RemoteIpAddress` в записях — внутренний адрес (`10.10.x.x` пода ingress или `192.168.0.x` подсети), а не адрес пользователя:
настоящий IP клиента до приложения сейчас не доходит, фильтр по IP бесполезен.

## Сценарии

- **Ошибки на проде за последний час:**
  ```powershell
  aws logs filter-log-events --profile selectel-logs --log-group-name k8s/joinrpg-prod --log-stream-names _all `
    --start-time ($now - 3600000) --filter-pattern ERROR --max-items 2000 --output json > err.json
  ```
  Свести в сводку, а не читать построчно:
  ```powershell
  (Get-Content err.json -Raw | ConvertFrom-Json).events | ForEach-Object { $_.message | ConvertFrom-Json } |
    Where-Object YcLevel -eq 'ERROR' |
    Group-Object { $_.kubernetes.container_name }, SourceContext, { (($_.message -split "`n")[0] -replace '\d+', 'N') } |
    Sort-Object Count -Descending | Select-Object -First 15 Count, Name
  ```
- **Одно приложение** — поток контейнера вместо `_all`: `--log-stream-names joinrpg-idportal`.
- **По TraceId/RequestId** из отчёта пользователя или страницы ошибки: `--filter-pattern '<traceid>'`,
  окно — сутки, если время неизвестно.

### Соседние действия того же пользователя или проекта

Найдя конкретный инцидент, почти всегда стоит посмотреть, что пользователь делал до и после: это объясняет
контекст (сам обновил страницу и получил успех, ошибка воспроизводится на каждом запросе и т. п.).
Окно — ±5–15 минут вокруг найденной записи:

- по пользователю: `--filter-pattern '"user@example.com"'` (поле `LoggedUser`);
- по проекту: терм с числом `ProjectId` и уточнение на клиенте по полю `ProjectId`
  (формат поля разный: число `1611` в логах запросов, строка `"Project(1611)"` в части scope-полей).

## Логи почты

Почту шлёт Yandex Postbox (`Postbox:Endpoint` в `appsettings.json` Portal/IdPortal), её логи — группа
`joinrpg-mail-logs` в Yandex Cloud Logging, читать через `yc logging read joinrpg-mail-logs --since 24h --format json`.
**С 2026-10-18 API чтения Yandex Logging недоступен** (`reader.logging.yandexcloud.net: context deadline exceeded`,
`Unavailable`), так что проверить доставку письма сейчас нечем. Старые k8s-логи Yandex (`joinrpg-k8s-logs`,
до 2026-10-15) недоступны тоже; копии, нужные для DRP, сохранены локально.

## Ограничения и осторожность

- Это боевые данные пользователей (email, содержимое запросов) — не публиковать вовне, не вставлять в публичные issue/PR.
- Широкое окно без терма тянет много данных — сужать окно, поток и терм, прежде чем брать всё подряд.
- Логи потерянных записей не догоняют: если fluent-bit не смог отправить пачку (например, упёрся в лимит
  потоков), записи пропадают. Подозрительная тишина — повод заглянуть в
  `kubectl logs -n infra ds/fluent-bit --tail=50`.
