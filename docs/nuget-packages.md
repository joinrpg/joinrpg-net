# NuGet-пакеты

Часть `src/Common` публикуется на nuget.org и подключается в других репозиториях.

## Пакеты

| Пакет | Что внутри | Зависит от наших пакетов |
|---|---|---|
| `JoinRpg.Common.PrimitiveTypes` | типы-значения: типизированные Id, `Email`, `UserInfoHeader` и т. п. | генератора: он восстанавливается у потребителя, но не запускается (`exclude="Build,Analyzers"` в nuspec) |
| `JoinRpg.Common.PrimitiveTypes.SourceGenerator` | генератор `[TypedEntityId]` и `[TypedStringValue]` | — |
| `JoinRpg.Common.EntityFrameworkCore` | EF Core-конвертеры для типизированных Id | PrimitiveTypes |
| `JoinRpg.Common.WebInfrastructure` | Serilog, OpenTelemetry, health checks, Data Protection, PostgreSQL, логин через id.joinrpg.ru | PrimitiveTypes |
| `JoinRpg.Common.WebComponents` | Blazor-компоненты, каталог — components.joinrpg.ru | PrimitiveTypes |
| `JoinRpg.Common.KogdaIgraClient` | HTTP-клиент к kogda-igra.ru | — |

Снаружи пакеты подключают [bastilia-rating](https://github.com/leotsarev/bastilia-rating) и
[TwilightRandom](https://github.com/leotsarev/TwilightRandom). Внутри решения те же проекты
подключаются ссылкой на проект, кроме генератора — его берут из NuGet (см.
[README генератора](../src/Common/JoinRpg.Common.PrimitiveTypes.SourceGenerator/README.md)).

## Как проект становится пакетом

Пакетом проект делает `<IsPackable>true</IsPackable>` в `.csproj` (по умолчанию `false`, см.
`Directory.Build.props`). Остальное добавляет `Directory.Build.targets`:

- адрес репозитория, лицензия MIT;
- `README.md` и `CHANGELOG.md` из каталога проекта, если они есть, кладутся в пакет. README
  показывается на nuget.org, а после восстановления лежит в `~/.nuget/packages/<id в нижнем регистре>/<версия>/`.

В `.csproj` пакета остаются `IsPackable`, `Description` (по-английски — для поиска на nuget.org)
и то, что отличает именно его. Значения из `Directory.Build.targets` пакет может переопределить.

Новый пакет нужно ещё добавить в матрицу `.github/workflows/nuget-publish.yml`.

## Версия и публикация

Все пакеты выходят одной версией. На `master` это последний тег `vГГГГ.N` плюс число коммитов
после него (`2026.9.438`). `Version` в `.csproj` не задавать: workflow её перекрывает.

Публикация ручная: запустить `.github/workflows/nuget-publish.yml` с `master` (с другой ветки
выйдет prerelease-версия с именем ветки). Он публикует все пакеты матрицы, номер версии — в логе
job'а (`...<версия>.nupkg`). На nuget.org пакет появляется через несколько минут.

CI пакует все пакеты на каждом PR (шаг «Pack NuGet packages»), сломанная упаковка видна до
публикации.

## Документация пакета

- `README.md` — для того, кто подключает пакет: зачем он, установка, пример, основные типы,
  конфигурация, ограничения. Как устроен пакет внутри и как его править — не сюда, а в `docs/`.
- `CHANGELOG.md` — что изменилось для потребителя. Наверху раздел «Не выпущено».
