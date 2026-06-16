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

### Способ 2: Командная строка (если настроены CLI tools)

Если у вас установлены Entity Framework CLI tools, можно использовать:

```bash
# Перейдите в папку проекта
cd src/JoinRpg.Dal.Impl

# Создайте миграцию (требуется EntityFramework.Commands)
dotnet ef migrations add AddНазваниеМиграции --context MyDbContext --output-dir Migrations
```

**Примечание**: В проекте не настроены `EntityFramework.Commands`, поэтому способ 2 может не работать. Используйте способ 1.

## Структура файлов миграций

Каждая миграция создает два файла в папке `src/JoinRpg.Dal.Impl/Migrations/`:

1. `{Timestamp}_{ИмяМиграции}.cs` — основной файл с методами `Up()` и `Down()`.
2. `{Timestamp}_{ИмяМиграции}.Designer.cs` — автоматически сгенерированный файл с метаданными миграции.

**Пример**:
- `202605081320406_AddProjectRolesList.cs`
- `202605081320406_AddProjectRolesList.Designer.cs`

**Важно**: Не редактируйте `.Designer.cs` файлы вручную — они генерируются автоматически. Все изменения вносите только в основной файл миграции.

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