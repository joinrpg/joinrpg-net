---
name: prod-logs
description: Читать и фильтровать логи продакшна и dev-окружения joinrpg.ru из Yandex Cloud Logging (k8s-логи Portal/IdPortal/ComponentBook, логи почты). Используй, когда нужно разобрать ошибку на проде, посмотреть последние запросы, найти конкретный трейс/RequestId или проверить, ушло ли письмо.
---

# Логи продакшна (Yandex Cloud Logging)

Логи собираются в Yandex Cloud Logging и читаются через CLI `yc` (уже установлен и авторизован в этом окружении, `yc config list` показывает активный токен/folder).

## Группы логов

```
yc logging group list
```

- `joinrpg-k8s-logs` — stdout всех подов k8s (портал, id-портал, componentbook), и dev, и prod вместе.
- `joinrpg-mail-logs` — доставка почты (Yandex Postbox): accepted/bounced/complaint и т.п.
- `joinrpg-direct-logs` — прочие прямые логи (на момент написания скилла пуст).

## Базовая команда

```
yc logging read <group-name> --since <since> [--until <until>] [--resource-types dev|prod] \
  [--levels ERROR,WARN,...] [--filter '<условие>'] [--limit N] --format json
```

- `--since`/`--until` принимают duration (`2h`, `30m`) или RFC-3339, **не** принимают человеческие фразы вроде "1 hour ago".
- Без `--format json` текстовый вывод часто показывает пустое сообщение для структурированных логов — **всегда используй `--format json`**, когда нужно видеть поля, а не только `message`.
- `--resource-types prod` / `--resource-types dev` — фильтр по namespace k8s (это и есть способ отличить прод от дева в `joinrpg-k8s-logs`).
- `--levels` фильтрует по `level` (`INFO`, `WARN`, `ERROR`, ...).
- `--filter` — условие по полям, например:
  ```
  --filter 'json_payload.kubernetes.container_name="joinrpg-portal"'
  ```
  Поле в фильтре — `json_payload...` (snake_case, как в JSON-выводе), не `jsonPayload`.

## Структура записи k8s-логов (`joinrpg-k8s-logs`)

```jsonc
{
  "resource": { "type": "prod", "id": "joinrpg-portal-d9b568fcf-27rsx" }, // type = namespace (dev/prod), id = pod name
  "timestamp": "...",
  "level": "WARN",
  "message": "человекочитаемое сообщение",
  "json_payload": {
    "kubernetes": {
      "container_name": "joinrpg-portal" | "joinrpg-idportal" | "joinrpg-componentbook",
      "pod_name": "...",
      "namespace_name": "dev" | "prod"
    },
    // для JoinRpg.Portal / JoinRpg.IdPortal — структурированные поля Serilog:
    "AppName": "JoinRpg.Portal",
    "Level": "Warning",
    "ActionName": "JoinRpg.Portal.Controllers.CharacterController.Details (...)",
    "RequestId": "0HNO22ICRNTV8:00000005",
    "RequestPath": "/158/character/14311/details",
    "TraceId": "...",
    "SpanId": "...",
    "SourceContext": "JoinRpg.Dal.Impl.MyDbContext",
    "fields": { "http.request.method": "GET", "url.path": "...", "sql": "...", ... }
  }
}
```

Полезные сценарии:

- **Ошибки на проде за последний час:**
  ```
  yc logging read joinrpg-k8s-logs --resource-types prod --levels ERROR --since 1h --format json
  ```
- **Логи конкретного приложения (портал):**
  ```
  yc logging read joinrpg-k8s-logs --resource-types prod \
    --filter 'json_payload.kubernetes.container_name="joinrpg-portal"' --since 2h --format json
  ```
  Названия контейнеров: `joinrpg-portal`, `joinrpg-idportal`, `joinrpg-componentbook`.
- **По конкретному RequestId/TraceId** (когда есть корреляционный id из отчёта пользователя):
  ```
  yc logging read joinrpg-k8s-logs --resource-types prod \
    --filter 'json_payload.TraceId="<traceid>"' --since 24h --format json
  ```
- **Живой хвост логов:** добавить `-f`/`--follow` (осторожно — блокирующая команда, не использовать без явной необходимости и таймаута).

### Соседние действия того же пользователя/проекта/IP

Когда разобрал конкретный инцидент (нашёл `TraceId`/`RequestId`), почти всегда полезно посмотреть, что пользователь делал до и после — это часто объясняет контекст (например, что он же вручную обновил страницу и получил успех, или что ошибка воспроизводится на каждом его запросе). Разворачивай окно времени вокруг найденной записи (`--since`/`--until` с запасом в 5-15 минут) и фильтруй по одному из идентификаторов:

- **По пользователю** — `LoggedUser` (email, если залогинен):
  ```
  yc logging read joinrpg-k8s-logs --resource-types prod \
    --filter 'json_payload.LoggedUser="user@example.com"' \
    --since "2026-08-25T17:30:00Z" --until "2026-08-25T17:45:00Z" --format json
  ```
- **По проекту** — `ProjectId` (внимание: формат разный в разных типах записей — число `1535` в HTTP-логах запроса/Serilog RequestLogging, но встречается и как строка `"Project(158)"` в некоторых scope-полях типа lazy-load warning; для фильтра по `--filter` работает числовое значение):
  ```
  yc logging read joinrpg-k8s-logs --resource-types prod \
    --filter 'json_payload.ProjectId=1535' --since 2h --format json
  ```
- **По IP** — `RemoteIpAddress` (совпадающий IP полезен, когда пользователь не залогинен или чтобы поймать все его сессии/аккаунты с одного адреса):
  ```
  yc logging read joinrpg-k8s-logs --resource-types prod \
    --filter 'json_payload.RemoteIpAddress="1.2.3.4"' --since 2h --format json
  ```
- Комбинировать условия `--filter` через `AND`/`OR` (CEL-подобный синтаксис) — например, сузить по пользователю и приложению одновременно:
  ```
  --filter 'json_payload.LoggedUser="user@example.com" AND json_payload.kubernetes.container_name="joinrpg-portal"'
  ```

## Структура почтовых логов (`joinrpg-mail-logs`)

```jsonc
{
  "resource": { "type": "postbox.identity" },
  "level": "INFO",
  "message": "Message accepted" | "Message bounced" | ...,
  "json_payload": {
    "identity_id": "...",
    "message": { "mail": { "from": "...", "to": "...", "subject": "..." }, "message_id": "..." }
  }
}
```

Проверить, ушло ли письмо конкретному пользователю:
```
yc logging read joinrpg-mail-logs --since 24h --format json --limit 100 | grep -i "<email>"
```
(фильтр по `json_payload.message.mail.to` через `--filter` тоже должен работать, если нужна точность).

## Ограничения и осторожность

- Это боевые данные пользователей (email, содержимое запросов) — не публиковать вовне, не вставлять в публичные issue/PR.
- Большие `--limit`/широкий `--since` могут вернуть много данных — уточняй условие (`--filter`, `--levels`, `--resource-types`) прежде чем тянуть всё подряд.
- `yc` при каждом вызове печатает предупреждение про network connectivity to tool initialization service — это не ошибка, можно игнорировать (или выставить `YC_CLI_INITIALIZATION_SILENCE=true`).
