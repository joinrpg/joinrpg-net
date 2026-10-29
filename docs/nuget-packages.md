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
[правку генератора](source-generator.md)).

## Как проект становится пакетом

Пакетом проект делает `<IsPackable>true</IsPackable>` в `.csproj` (по умолчанию `false`, см.
`Directory.Build.props`). Остальное добавляет `Directory.Build.targets`:

- адрес репозитория, лицензия MIT;
- `README.md` и `CHANGELOG.md` из каталога проекта кладутся в пакет, без README
  `dotnet pack` падает. README показывается на nuget.org, а после восстановления лежит в
  `~/.nuget/packages/<id в нижнем регистре>/<версия>/`;
- PublicApiAnalyzers: публичный API перечислен в `PublicAPI.*.txt`, см.
  [Публичный API](#публичный-api);
- package validation: CI сравнивает пакет с последней версией на nuget.org, см.
  [Ломающее изменение](#ломающее-изменение).

В `.csproj` пакета остаются `IsPackable`, `Description` (по-английски — для поиска на nuget.org)
и то, что отличает именно его. Значения из `Directory.Build.targets` пакет может переопределить.

Новый пакет нужно ещё добавить в матрицу `.github/workflows/nuget-publish.yml` и запустить
`scripts/Update-PublicApi.ps1`.

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
- `CHANGELOG.md` — что изменилось для потребителя, по мотивам
  [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/).

## CHANGELOG

Запись добавляется в том же PR, что и изменение, в раздел «Не выпущено» каждого затронутого
пакета. Подразделы — «Ломающие изменения», «Добавлено», «Изменено», «Удалено», «Исправлено»,
пустые не пишутся.

- Пишется то, что видит потребитель: публичные типы и члены, параметры компонентов, поведение,
  формат, статика в `wwwroot`, минимальные версии зависимостей — в том числе бампы Dependabot:
  запись за них добавляет тот, кто вливает бамп. Рефакторинг, тесты и правки внутри
  `internal` — нет.
- Одна короткая строка: тип или компонент и что изменилось, номер PR в конце. Подробности — в
  README или PR.
- Ломающее изменение — после которого код потребителя не соберётся или перестанет работать
  сценарий, который работал. У каждого — «Переход: ...».
- Правка генератора доходит до API `JoinRpg.Common.PrimitiveTypes` только после бампа генератора
  в `Directory.Packages.props`: запись в PrimitiveTypes — в PR с бампом.

Если PR меняет `PublicAPI.*.txt` пакета, а его `CHANGELOG.md` — нет, CI (job «NuGet changelog»)
падает.

## Публичный API

Публичный API каждого пакета, кроме генератора, перечислен в `PublicAPI.Shipped.txt` и
`PublicAPI.Unshipped.txt` рядом с `.csproj` (формат —
[PublicApiAnalyzers](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md)).
Публичный символ без строки в этих файлах — ошибка RS0016, строка без символа — RS0017, поэтому
изменение API видно в диффе PR.

Файлы правит скрипт: код-фикс анализатора пропускает сгенерированный код (Razor-компоненты,
`[TypedEntityId]`).

```powershell
pwsh scripts/Update-PublicApi.ps1                       # все пакеты
pwsh scripts/Update-PublicApi.ps1 -Project src/Common/JoinRpg.Common.PrimitiveTypes/JoinRpg.Common.PrimitiveTypes.csproj
```

Новое попадает в `Unshipped`, удалённое из `Shipped` — строкой `*REMOVED*...` в `Unshipped`.
В `Shipped` — опубликованное на nuget.org; после публикации строки из `Unshipped` переносятся
туда (пока вручную). После скрипта проверять `dotnet build --no-incremental`: инкрементальная
сборка может не показать RS0016.

## Ломающее изменение

CI падает с ошибкой CP*, если код, собранный против опубликованной версии, перестанет
компилироваться или запускаться с новой (CP0002 — удалён публичный метод или свойство). Если так и
задумано — запиши изменение в «Ломающие изменения» CHANGELOG и создай рядом с `.csproj` файл
`CompatibilitySuppressions.xml`:

```powershell
dotnet pack <путь к .csproj> -c Release -p:Version=9999.0.0 -p:PackageValidationBaselineVersion=<последняя версия на nuget.org> -p:GenerateCompatibilitySuppressionFile=true
```

`Version` — любая выше опубликованной, без неё в файл попадёт лишний CP0003.

После публикации файл устаревает: базой становится новая версия. Ошибкой это не считается; удалить файл можно в
любом следующем PR. Генератор так не проверяется — у его пакета нет `lib/`, сравнивать нечего.
