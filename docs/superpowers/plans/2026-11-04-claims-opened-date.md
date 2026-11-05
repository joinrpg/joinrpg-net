# Дата первого открытия заявок (ClaimsOpenedDate) — план реализации

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Хранить в проекте дату первого открытия заявок: колонка `Projects.ClaimsOpenedDate` (nullable, бэкфилл), поле `ProjectClaimSettings.ClaimsOpenedDate` в `ProjectInfo`, заполнение при первом включении приёма заявок.

**Architecture:** Поле живёт в EF6-сущности `Project` (SQL Server, см. `docs/ef-migrations.md`). Бэкфилл существующих данных — один T-SQL в миграции. В доменных типах дата — `DateOnly?` внутри рекорда `ProjectClaimSettings` (по образцу `CreateDate`). Заполнение при работе сайта — в `ProjectService.SetClaimSettings` при переходе `IsAcceptingClaims` в `true`, только если поле ещё пустое.

**Tech Stack:** EF6 Code-First миграции, .NET 10, xUnit + Shouldly (тесты по образцу `ProjectServiceTest`).

**Spec:** `docs/superpowers/specs/2026-11-04-claims-opened-date-design.md`

## Global Constraints

- Работать только в текущем worktree `C:/Users/leo/source/repos/leotsarev/joinrpg-net.claim-open-date`.
- Коммитить через `git commit -m` (без `--edit`); перед коммитом `dotnet format`.
- Сравнение с базовой веткой — через `upstream`: `git diff upstream/master...HEAD`.
- Новые поля DataModel согласованы с @leotsarev (см. spec).
- Миграция EF6 идёт только в SQL Server: raw SQL в `Sql(...)` — T-SQL по образцу соседних миграций (без `[]`-скобок не обязательно, но стиль — как у соседей).
- `.Designer.cs` и `.resx` миграции не править руками.
- Строки UI не добавляются (поле техническое).

## Review Focus

- Проект, у которого заявки никогда не открывались (нет заявок, `IsAcceptingClaims = 0`) → дата остаётся `NULL` — покрыто бэкфиллом в Task 1 и тестом `SetClaimSettings_Closed_KeepsNull` в Task 3.
- Проект с заявками, которые уже закрыты → бэкфилл берёт `MIN(Claims.CreateDate)`, а не дату миграции — бэкфилл Task 1 (два UPDATE: сначала по заявкам, потом по открытым без заявок).
- Повторное открытие после закрытия не сдвигает дату — тест `SetClaimSettings_Reopen_KeepsClaimsOpenedDate` в Task 3.
- Заблокированный проект (ADR023): настройки заявок доступны; включение приёма в блокировке — это реальное открытие, дата заполняется (семантика «первый раз открыты»).
- Клиенты `ProjectClaimSettings` с позиционными аргументами (тестовые фикстуры) — новый параметр имеет default `null`, они компилируются без правок; проверяется сборкой в Task 2.

---

### Task 1: DataModel + EF6-миграция с бэкфиллом + db-schema.md

**Files:**
- Modify: `src/JoinRpg.DataModel/Projects/Project.cs` (после `IsAcceptingClaims`, строка 19)
- Create: `src/JoinRpg.Dal.Impl/Migrations/<timestamp>_AddProjectClaimsOpenedDate.cs` (+ `.Designer.cs`, `.resx` — генерируются)
- Modify: `docs/db-schema.md` (erDiagram `Projects`, строки 93–101)

**Interfaces:**
- Consumes: —
- Produces: колонка `dbo.Projects.ClaimsOpenedDate` (datetime, nullable); свойство `Project.ClaimsOpenedDate` типа `DateTime?` — Task 2 и Task 3 используют его.

- [ ] **Step 1: Добавить свойство в DataModel**

В `src/JoinRpg.DataModel/Projects/Project.cs` после `IsAcceptingClaims` добавить:

```csharp
/// <summary>
/// Дата, когда заявки в проект были открыты в первый раз. NULL — заявки ещё ни разу не открывались.
/// Заполняется один раз при первом включении приёма заявок; повторные открытия/закрытия не меняют.
/// </summary>
public DateTime? ClaimsOpenedDate { get; set; }
```

- [ ] **Step 2: Сгенерировать миграцию через ef6 (Способ 2 из docs/ef-migrations.md)**

Требуется запущенный SQL Server из `docker compose up -d` и собранный `src/Joinrpg.Dal.Migrate`. По документу `docs/ef-migrations.md` (строки 31–55): накатить существующие миграции на временную БД `joinrpg_scratch`, затем

```bash
ef6 migrations add AddProjectClaimsOpenedDate --project-dir <repo>/src/JoinRpg.Dal.Impl --root-namespace JoinRpg.Dal.Impl
```

- [ ] **Step 3: Переписать `Up()` в стиле соседних миграций и добавить бэкфилл**

Основной файл — file-scoped namespace, без BOM (иначе `dotnet format` падает с `CHARSET`). `Up()`:

```csharp
AddColumn("dbo.Projects", "ClaimsOpenedDate", c => c.DateTime(nullable: true));
// Бэкфилл: дата первой отправленной заявки.
Sql("UPDATE dbo.Projects SET ClaimsOpenedDate = (SELECT MIN(c.CreateDate) FROM dbo.Claims c WHERE c.ProjectId = dbo.Projects.ProjectId) "
    + "WHERE EXISTS (SELECT 1 FROM dbo.Claims c WHERE c.ProjectId = dbo.Projects.ProjectId)");
// Заявок не было, но приём сейчас открыт — считаем датой открытия момент миграции.
Sql("UPDATE dbo.Projects SET ClaimsOpenedDate = SYSUTCDATETIME() WHERE ClaimsOpenedDate IS NULL AND IsAcceptingClaims = 1");
```

`Down()` — `DropColumn("dbo.Projects", "ClaimsOpenedDate")`.

- [ ] **Step 4: Проверить миграцию на временной базе**

```bash
ef6 database update
ef6 database update --target AddPlotElementRolesList   # предыдущая миграция
ef6 database update
```

Ожидается: накат и откат без ошибок. Дополнительно `SELECT ClaimsOpenedDate FROM dbo.Projects` не обязателен — проверка через тест в Step 5.

- [ ] **Step 5: Обновить docs/db-schema.md и прогнать DbSchemaDocumentationScenario**

В erDiagram `Projects` (после `IsAcceptingClaims`) добавить:

```
datetime ClaimsOpenedDate "дата первого открытия заявок, NULL — ещё не открывались"
```

```bash
docker compose up -d
dotnet test src/JoinRpg.IntegrationTest --filter "FullyQualifiedName~DbSchemaDocumentationScenario"
```

Expected: PASS (интеграционный тест здесь обязателен — изменилась схема БД, см. AGENTS.md). Удалить временную БД `joinrpg_scratch` командой из `docs/ef-migrations.md` (строки 71–76).

- [ ] **Step 6: Commit**

```bash
dotnet format
git add src/JoinRpg.DataModel/Projects/Project.cs src/JoinRpg.Dal.Impl/Migrations docs/db-schema.md
git commit -m "feat: добавить Projects.ClaimsOpenedDate — дату первого открытия заявок, с бэкфиллом"
```

### Task 2: ProjectClaimSettings.ClaimsOpenedDate + маппинг в ProjectMetadataRepository

**Files:**
- Modify: `src/JoinRpg.DomainTypes/ProjectMetadata/ProjectInfo.cs` (рекорд `ProjectClaimSettings`, строка 406)
- Modify: `src/JoinRpg.Dal.Impl/Repositories/ProjectMetadataRepository.cs:76-82`

**Interfaces:**
- Consumes: `Project.ClaimsOpenedDate` (`DateTime?`) из Task 1.
- Produces: `ProjectClaimSettings(..., DateOnly? ClaimsOpenedDate = null)` — последний позиционный параметр рекорда с default `null`; `Result.ClaimSettings.ClaimsOpenedDate` читается в тестах Task 3.

- [ ] **Step 1: Добавить параметр в рекорд**

В `src/JoinRpg.DomainTypes/ProjectMetadata/ProjectInfo.cs` рекорд становится:

```csharp
public record ProjectClaimSettings(
    CharacterIdentification? DefaultTemplate,
    bool StrictlyOneCharacter,
    bool AutoAcceptClaims,
    bool IsAcceptingClaims,
    bool IsPublicProject,
    DateOnly? ClaimsOpenedDate = null);
```

Default `null` — чтобы позиционные вызовы в тестовых фикстурах (`ProjectInfoFixture.cs:61` и др.) компилировались без правок.

- [ ] **Step 2: Пробросить поле в маппинге**

В `src/JoinRpg.Dal.Impl/Repositories/ProjectMetadataRepository.cs` в `new ProjectClaimSettings(...)` добавить последним named-аргументом:

```csharp
ClaimsOpenedDate: project.ClaimsOpenedDate is { } opened ? DateOnly.FromDateTime(opened) : null
```

(по образцу `createDate: DateOnly.FromDateTime(project.CreatedDate)` на строке 72).

- [ ] **Step 3: Сборка и существующие тесты**

```bash
dotnet build
dotnet test src/JoinRpg.DomainTypes.Test
dotnet test src/JoinRpg.Services.Impl.Test
```

Expected: сборка без ошибок (все `new ProjectClaimSettings` с пятью аргументами берут default), тесты PASS — особенно существующий `SetClaimSettings_AppliesSettings` в `ProjectServiceTest`, где `Result` — пересобранный через реальный репозиторий `ProjectInfo`.

- [ ] **Step 4: Commit**

```bash
dotnet format
git add src/JoinRpg.DomainTypes/ProjectMetadata/ProjectInfo.cs src/JoinRpg.Dal.Impl/Repositories/ProjectMetadataRepository.cs
git commit -m "feat: ClaimsOpenedDate в ProjectClaimSettings и маппинг из Project"
```

### Task 3: Заполнение даты в SetClaimSettings + тесты

**Files:**
- Modify: `src/JoinRpg.Services.Impl/Projects/ProjectService.cs` (метод `SetClaimSettings`, строки 228–238)
- Test: `src/JoinRpg.Services.Impl.Test/Projects/ProjectServiceTest.cs`

**Interfaces:**
- Consumes: `Project.ClaimsOpenedDate`, `ProjectClaimSettings.ClaimsOpenedDate` из Task 1–2; контекст мутации `ctx.Now` (`ProjectMutationContext<TArgs>.Now`, `DateTimeOffset`) — `src/JoinRpg.Services.Impl/Projects/ProjectOperationContext.cs:42`.
- Produces: поведение «дата ставится один раз при первом включении приёма заявок».

- [ ] **Step 1: Написать failing-тесты**

В `src/JoinRpg.Services.Impl.Test/Projects/ProjectServiceTest.cs` добавить (по образцу соседних тестов; `mock.Project` стартует с `IsAcceptingClaims = true` — см. `MockedProject.cs:265`):

```csharp
[Fact]
public async Task SetClaimSettings_ClaimsClosedToOpen_FillsClaimsOpenedDate()
{
    mock.Project.IsAcceptingClaims = false;
    mock.ReInitProjectInfo();

    await CreateService().SetClaimSettings(ProjectId,
        mock.ProjectInfo.ClaimSettings with { IsAcceptingClaims = true });

    mock.Project.ClaimsOpenedDate.ShouldNotBeNull();
    Result.ClaimSettings.ClaimsOpenedDate.ShouldBe(DateOnly.FromDateTime(mock.Project.ClaimsOpenedDate.Value));
}

[Fact]
public async Task SetClaimSettings_Reopen_KeepsClaimsOpenedDate()
{
    var firstOpened = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
    mock.Project.ClaimsOpenedDate = firstOpened;
    mock.ReInitProjectInfo();

    await CreateService().SetClaimSettings(ProjectId,
        mock.ProjectInfo.ClaimSettings with { IsAcceptingClaims = false });
    await CreateService().SetClaimSettings(ProjectId,
        mock.ProjectInfo.ClaimSettings with { IsAcceptingClaims = true });

    mock.Project.ClaimsOpenedDate.ShouldBe(firstOpened);
    Result.ClaimSettings.ClaimsOpenedDate.ShouldBe(DateOnly.FromDateTime(firstOpened));
}

[Fact]
public async Task SetClaimSettings_Closed_KeepsNull()
{
    await CreateService().SetClaimSettings(ProjectId,
        mock.ProjectInfo.ClaimSettings with { IsAcceptingClaims = false });

    mock.Project.ClaimsOpenedDate.ShouldBeNull();
    Result.ClaimSettings.ClaimsOpenedDate.ShouldBeNull();
}
```

- [ ] **Step 2: Прогнать тесты — убедиться, что падают**

```bash
dotnet test src/JoinRpg.Services.Impl.Test --filter "FullyQualifiedName~SetClaimSettings_ClaimsClosedToOpen_FillsClaimsOpenedDate"
```

Expected: FAIL — `ClaimsOpenedDate` остаётся `null` (свойство ещё никто не заполняет).

- [ ] **Step 3: Реализовать заполнение в `SetClaimSettings`**

В `src/JoinRpg.Services.Impl/Projects/ProjectService.cs` в лямбде `ctx` после строки `ctx.Project.IsAcceptingClaims = ...` добавить:

```csharp
// Дата первого открытия заявок фиксируется один раз (#задача): при повторных открытиях не меняется.
if (ctx.Project.IsAcceptingClaims && ctx.Project.ClaimsOpenedDate is null)
{
    ctx.Project.ClaimsOpenedDate = ctx.Now.UtcDateTime;
}
```

- [ ] **Step 4: Прогнать тесты проекта**

```bash
dotnet test src/JoinRpg.Services.Impl.Test
```

Expected: все PASS, включая три новых.

- [ ] **Step 5: Полная проверка и commit**

```bash
dotnet build
dotnet test src/JoinRpg.Domain.Test
dotnet test src/JoinRpg.Dal.Impl.Test
dotnet format
```

Expected: сборка и тесты PASS. Затем:

```bash
git add src/JoinRpg.Services.Impl/Projects/ProjectService.cs src/JoinRpg.Services.Impl.Test/Projects/ProjectServiceTest.cs
git commit -m "feat: заполнять ClaimsOpenedDate при первом открытии заявок"
```

## После реализации

- Ревью диффа отдельным субагентом (обязательно по AGENTS.md): `git diff upstream/master...HEAD`.
- PR не создаётся, пока пользователь не попросит (или дальнейшие указания).
