---
name: selectel-readonly
description: Читать настройки облака Selectel (проект JoinRpgCloud) через API от сервисного пользователя с ролью reader — кластер Managed Kubernetes, Container Registry, бакеты S3, сети, балансировщики. Используй, чтобы посмотреть, как что-то настроено в Selectel, не трогая панель. Только чтение, никаких изменений.
---

# Readonly-доступ к API Selectel

Все запросы — только через скрипт `selectel-api.ps1` из этой папки. Он сам выпускает IAM-токен,
делает один GET и печатает JSON. Токен и пароль не выводит и не сохраняет, ходит только на хосты
Selectel (`*.selcloud.ru`, `*.selectel.ru`, `*.servercore.com`). Методов кроме GET в нём нет.

**Не собирать токен вручную** (`POST .../identity/v3/auth/tokens` из своего PowerShell/curl):
автофильтр Claude Code справедливо режет это как работу с учётными данными, и так пароль
оказывается в командах сессии.

## Учётная запись

Сервисный пользователь Selectel с ролью **`reader`** на аккаунт: видит всё, чем управляет `member`,
но ничего не может менять. Пользователей и их роли не видит (это отдельная роль `iam.viewer`).
Отчёты, транзакции и тикеты недоступны: им нужен статический токен, а его выдают только
пользователям панели.

Учётные данные — пользовательские переменные окружения Windows на машине пользователя,
не в репозитории и не в чате:

- `SELECTEL_ACCOUNT_ID` — номер аккаунта (правый верхний угол панели)
- `SELECTEL_USERNAME` — имя сервисного пользователя
- `SELECTEL_PASSWORD` — его пароль

Скрипт читает их из реестра (`[Environment]::GetEnvironmentVariable(..., 'User')`), поэтому
перезапуск сессии после `SetEnvironmentVariable` не нужен. Если переменных нет — попроси
пользователя их задать, не проси продиктовать пароль.

Как завести пользователя: панель → **IAM** → **Сервисные пользователи** →
«Добавить сервисного пользователя», разрешение «Аккаунт» → роль `reader`.

## Разрешение на запуск

Чтобы скрипт запускался без подтверждений, в **личных** настройках `~/.claude/settings.json`
(не в `.claude/settings.json` репозитория — учётные данные личные):

```json
"permissions": { "allow": [
  "Bash(pwsh -NoProfile -File .claude/skills/selectel-readonly/selectel-api.ps1:*)"
] }
```

Путь относительный, поэтому вызывать скрипт надо из корня репозитория (любого worktree).

## Вызов

```bash
S=".claude/skills/selectel-readonly/selectel-api.ps1"
pwsh -NoProfile -File $S -Catalog                                        # сервисы и URL по регионам
pwsh -NoProfile -File $S -Service managed-kubernetes -Region ru-6 -Path clusters
pwsh -NoProfile -File $S -Service container-registry -Path registries
pwsh -NoProfile -File $S -Service object-store -Region ru-6 -Path '?format=json'   # бакеты
pwsh -NoProfile -File $S -Url https://cloud.api.selcloud.ru/identity/v3/auth/projects
```

- `-Service` — тип сервиса из `-Catalog` (`managed-kubernetes`, `container-registry`, `object-store`,
  `load-balancer`, `network`, `compute`, `managed-database`, `dnsv2`, …). Без `-Region` запрос
  уходит во все регионы сервиса.
- `-Path` — путь после базового URL сервиса, **без ведущего `/`**: Git Bash переписывает `/clusters`
  в `C:/Program Files/Git/clusters`. Скрипт добавит `/` сам.
- `-Scope account` — токен на аккаунт вместо проекта (по умолчанию проект `JoinRpgCloud`).
  В токене на аккаунт роль показывается как `nobody`; для ресурсов проекта нужен токен на проект.
- Ответ — JSON целиком; длинные ответы фильтровать `grep`'ом по нужным полям.

## Что где

- **Managed Kubernetes** — регион **ru-6**, кластер `joinrpg`. Эндпоинты API:
  [docs.selectel.ru/api/managed-kubernetes](https://docs.selectel.ru/api/managed-kubernetes/).
- **Container Registry** — реестр `joinrpg`, сервис `container-registry`, путь `registries`.
- **Объектное хранилище (S3)** — бакеты в **ru-6** (`joinrpg-prod-avatars`, `joinrpg-dev-avatars`,
  бэкапы, `joinrpg-tfstate`, статические сайты). Список бакетов отдаёт Swift API сервиса
  `object-store`; в регионах, где хранилище не подключено, ответ 403.

Содержимое бакетов с бэкапами и `joinrpg-tfstate` не читать без прямой просьбы пользователя:
там персональные данные и секреты инфраструктуры.

Справочник API: [docs.selectel.ru/api](https://docs.selectel.ru/api/), роли —
[справочник ролей](https://docs.selectel.ru/access-control/role-reference/).
