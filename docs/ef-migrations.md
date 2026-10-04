# Работа с миграциями Entity Framework 6 (Code-First)

В проекте используется **Entity Framework 6** (не Entity Framework Core) Code-First подход с ручным управлением миграциями.

## Создание миграции

После добавления или изменения сущности в `JoinRpg.DataModel` необходимо создать миграцию.

### Способ 1: Visual Studio Package Manager Console (рекомендуемый)

1. Откройте проект в Visual Studio
2. Откройте Package Manager Console: **Tools → NuGet Package Manager → Package Manager Console**
3. Убедитесь, что в выпадающем списке "Default project" выбран `JoinRpg.Dal.Impl`
4. Выполните команду:

```powershell
Add-Migration AddНазваниеМиграции
```

Где `AddНазваниеМиграции` — описательное имя миграции (например, `AddProjectRolesList`).

### Способ 2: Командная строка через `ef6.dll` (без Visual Studio)

`dotnet ef` из `.config/dotnet-tools.json` — инструмент EF Core, для EF6 он не подходит. Но в пакете
`EntityFramework` 6.3+ лежит свой CLI, `ef6.dll`. Это тот же движок, что и у `Add-Migration`: он
генерирует `.Designer.cs` и `.resx` со снапшотом модели. Так генерировались, например, миграции в #5240.

Скаффолдер сверяет модель с базой, где накатаны все миграции. Общую локальную базу `joinrpg` для этого
не трогайте: берите отдельную временную, например `joinrpg_scratch`.

```bash
dotnet build src/Joinrpg.Dal.Migrate
cd artifacts/bin/Joinrpg.Dal.Migrate/debug

EF=~/.nuget/packages/entityframework/6.5.2/tools/net6.0/any/ef6.dll   # версия — как в Directory.Packages.props
CS="Data Source=127.0.0.1;User Id=sa;Password=MsSqlPass1!;Initial Catalog=joinrpg_scratch;TrustServerCertificate=True"
ef6() {
  dotnet exec --depsfile Joinrpg.Dal.Migrate.deps.json --runtimeconfig Joinrpg.Dal.Migrate.runtimeconfig.json "$EF" "$@" \
    -a JoinRpg.Dal.Impl.dll --migrations-config JoinRpg.Dal.Impl.Migrations.Configuration \
    --connection-string "$CS" --connection-provider System.Data.SqlClient
}

# 1. Накатить существующие миграции на временную базу (создастся сама).
#    В конце будет «Unable to update database to match the current model because there are pending changes» — это норма,
#    так ef6 сообщает о ваших изменениях модели.
ef6 database update

# 2. Сгенерировать миграцию.
ef6 migrations add AddНазваниеМиграции --project-dir <путь к репозиторию>/src/JoinRpg.Dal.Impl --root-namespace JoinRpg.Dal.Impl

# 3. Проверить Up() и Down(): накатить, откатить к предыдущей миграции, снова накатить.
ef6 database update
ef6 database update --target ПредыдущаяМиграция
ef6 database update
```

После генерации:

- Основной `.cs` переписать в стиле соседних миграций: file-scoped namespace и **без BOM** (иначе
  `dotnet format` упадёт с `CHARSET`). `.Designer.cs` с BOM трогать не нужно, у соседей так же.
- NOT NULL-колонке на существующей таблице нужен `defaultValue`, иначе миграция упадёт на имеющихся строках.
  Для строки с кириллицей `defaultValue` не подходит: EF6 пишет литерал без префикса `N`, и результат зависит
  от collation базы. Добавьте колонку nullable, заполните через `Sql("UPDATE … SET X = N'…'")`, затем
  `AlterColumn` в NOT NULL.
- Явная `IndexAnnotation` на колонке внешнего ключа отключает конвенционный индекс `IX_<Колонка>`, и скаффолдер
  сгенерирует его удаление. Добавьте `new IndexAttribute("IX_<Колонка>")` в ту же аннотацию, а лишнюю пару
  Drop/Create того же индекса уберите руками.
- Снапшоты моделей линейны. Пока в другом открытом PR есть ещё не влитая миграция, ставьте свою ветку поверх
  неё; после её мерджа сделайте `git rebase --onto`.

Удалить временную базу (в Git Bash без `MSYS_NO_PATHCONV=1` путь к `sqlcmd` испортится):

```bash
MSYS_NO_PATHCONV=1 docker exec joinrpg-net-sql-server-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'MsSqlPass1!' -C \
  -Q "ALTER DATABASE joinrpg_scratch SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE joinrpg_scratch;"
```

## Структура файлов миграций

Каждая миграция создает три файла в папке `src/JoinRpg.Dal.Impl/Migrations/`:

1. `{Timestamp}_{ИмяМиграции}.cs` — основной файл с методами `Up()` и `Down()`.
2. `{Timestamp}_{ИмяМиграции}.Designer.cs` — автоматически сгенерированный файл с метаданными миграции.
3. `{Timestamp}_{ИмяМиграции}.resx` — снапшот модели (`Target`), с ним скаффолдер сравнивает модель при следующей миграции.

**Пример**:
- `202605081320406_AddProjectRolesList.cs`
- `202605081320406_AddProjectRolesList.Designer.cs`
- `202605081320406_AddProjectRolesList.resx`

**Важно**: Не редактируйте `.Designer.cs` и `.resx` вручную — они генерируются автоматически. Все изменения вносите только в основной файл миграции.
Поэтому миграцию, которая меняет модель, нельзя написать руками целиком: без снапшота следующий
скаффолдинг снова сгенерирует те же изменения.

## Обновление схемы в документации

**Обязательно тем же PR**: после создания миграции обновите [db-schema.md](db-schema.md) —
это описание схемы со всеми таблицами и колонками, оно поддерживается вручную.

Расхождение ловит интеграционный тест `DbSchemaDocumentationScenario`: он поднимает базу
в контейнере, накатывает миграции и сверяет `INFORMATION_SCHEMA` с документом. Тест гоняется
на CI, так что PR с новой миграцией и необновлённой схемой будет красным. Локально:

```bash
dotnet test src/JoinRpg.IntegrationTest --filter "FullyQualifiedName~DbSchemaDocumentationScenario"
```

Подробнее, что и как править (в том числе как поручить это агенту) —
[db-schema-regenerate.md](db-schema-regenerate.md).

## Применение миграций

Миграции применяются через отдельный проект `Joinrpg.Dal.Migrate`:

```bash
dotnet run --project src/Joinrpg.Dal.Migrate
```

Этот проект запускает все pending миграции для базы данных, указанной в `appsettings.json`.

## Проверка состояния миграций

Чтобы увидеть, какие миграции уже применены, можно использовать:

### В Package Manager Console:

```powershell
Get-Migrations
```

Эта команда покажет все миграции и отметит те, которые уже применены к БД.

### Прямой просмотр в БД:

Миграции хранятся в таблице `__MigrationHistory` в базе данных. Можно выполнить SQL-запрос:

```sql
SELECT MigrationId, Model, ProductVersion FROM __MigrationHistory ORDER BY MigrationId DESC;
```

## Откат миграции

В случае необходимости отката до предыдущей миграции:

### В Package Manager Console:

```powershell
Update-Database -TargetMigration ПредыдущаяМиграция
```

Где `ПредыдущаяМиграция` — имя миграции, к которой нужно откатиться (например, `AddProjectRolesList`).

Чтобы откатить все миграции и начать с чистого состояния:

```powershell
Update-Database -TargetMigration $InitialDatabase
```

## Особенности

1. **Контекст базы данных**: `MyDbContext` находится в `JoinRpg.Dal.Impl`.
2. **Папка миграций**: `src/JoinRpg.Dal.Impl/Migrations/` — содержит файлы миграций.
3. **Строка подключения**: Настраивается в `appsettings.json` проекта `Joinrpg.Dal.Migrate` (обычно используется `DefaultConnection`).
4. **Движок БД**: `MyDbContext` (EF6) работает поверх **SQL Server** — строка подключения `DefaultConnection` (`Initial Catalog=joinrpg`), миграции применяются через провайдер `System.Data.SqlClient` (см. `Joinrpg.Dal.Migrate/Ef6/JoinMigrationsConfig.cs`). Более новые DbContext-ы (DataProtection, DailyJob, Notifications, IdPortal) — это EF Core поверх **PostgreSQL** и к данному документу не относятся. О планах объединения см. [adr009-efcore-migration.md](adr009-efcore-migration.md).

## Типичные проблемы

- **Миграция не создается**: Убедитесь, что в Package Manager Console выбран правильный проект (`JoinRpg.Dal.Impl`). Также проверьте, что пакет `EntityFramework` установлен в проект.
- **Ошибка подключения к БД**: Проверьте, что Docker-контейнеры с базами данных запущены (`docker compose up -d`).
- **Конфликт имен миграций**: Если миграция с таким именем уже существует, выберите другое имя.
- **Команды не найдены в Package Manager Console**: Убедитесь, что проект `JoinRpg.Dal.Impl` загружен и что Entity Framework правильно установлен. Иногда помогает перезагрузка Visual Studio.
- **Ошибка "No migration configuration type was found"**: Убедитесь, что в проекте существует класс `Configuration` в папке `Migrations`.