# Доступ техподдержки в архивные проекты — план реализации

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `IProjectAccessService.GrantFullAccess` (кнопка админа «Поставить себе доступ») должна работать и в архивных проектах; обычный `GrantAccess` в архиве по-прежнему запрещён.

**Architecture:** В `ProjectAccessService` выделяется приватное ядро выдачи доступа с параметром `ProjectActiveRequirement`. Публичный `GrantAccess` передаёт `AllowBlocked` (без изменений), `GrantFullAccess` — `AllowArchivedOrBlocked`.

**Tech Stack:** C#, ASP.NET Core, xUnit + Shouldly (тесты `JoinRpg.Services.Impl.Test`), Autofac/DI без изменений.

**Spec:** `docs/superpowers/specs/2026-11-03-grant-access-archived-design.md`

## Global Constraints

- Правки `.cs` только через редактор (Edit), не потоковыми утилитами; CRLF.
- Перед коммитом: `dotnet format` (обязательно по AGENTS.md).
- Коммиты: `git commit -m "сообщение"`, без `--edit`.
- Юнит-тесты: `dotnet test src/JoinRpg.Services.Impl.Test`. Интеграционные тесты локально не гонять.

## Review Focus

- Админ с уже существующей строкой `ProjectAclStatus.Removed` в архивном проекте — реактивируется та же строка (путь `else` в ядре), а не создаётся дубликат. Тест ниже фиксирует именно создание; путь реактивации покрыт существующим `GrantFullAccess_AdminWithRemovedRow_ReactivatesSameRow` в том же файле.
- Имя операции в логе: после выделения ядра в логе должны остаться `GrantAccess` / `GrantFullAccess`, а не имя ядра. Проверяется глазами: `operationName` передаётся явно.
- Блокированный проект: существующий `GrantAccess_BlockedProject_Succeeds` должен остаться зелёным.

---

### Task 1: Ядро GrantAccess с требованием активности + тест

**Files:**
- Modify: `src/JoinRpg.Services.Impl/Projects/Metadata/ProjectAccessService.cs` (метод `GrantAccess`, строки 19–56; метод `GrantFullAccess`, строки 229–241)
- Test: `src/JoinRpg.Services.Impl.Test/Projects/ProjectAccessServiceTest.cs` (рядом с `GrantAccess_ArchivedProject_Throws`, строка 104)

**Interfaces:**
- Consumes: `ProjectPropsService.ChangeProjectProperties<TArgs>(ProjectIdentification, Permission?, ProjectActiveRequirement, TArgs, Action<ProjectMutationContext<TArgs>>, [CallerMemberName] string operationName = "")` — `operationName` обязателен, иначе в лог уйдёт имя ядра.
- Produces: поведение `GrantFullAccess(ProjectIdentification)` — работает в архивном проекте под админом. Публичные сигнатуры `IProjectAccessService` не меняются.

- [ ] **Step 1: Write the failing test**

В `ProjectAccessServiceTest.cs` после `GrantAccess_ArchivedProject_Throws`:

```csharp
[Fact]
public async Task GrantFullAccess_ArchivedProject_Succeeds()
{
    mock.Project.Active = false;
    mock.Project.IsAcceptingClaims = false;
    var service = CreateService(99, isAdmin: true);

    await service.GrantFullAccess(ProjectId);

    var acl = mock.Project.ProjectAcls.Single(a => a.UserId == 99);
    acl.Status.ShouldBe(ProjectAclStatus.Active);
    acl.CanGrantRights.ShouldBeTrue();
    acl.Role.ShouldBe("Техподдержка joinrpg.ru");
    unitOfWork.SaveChangesCallCount.ShouldBe(1);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/JoinRpg.Services.Impl.Test --filter "FullyQualifiedName~GrantFullAccess_ArchivedProject_Succeeds"`
Expected: FAIL — `ProjectDeactivatedException` (архивный проект).

- [ ] **Step 3: Реализовать ядро в `ProjectAccessService.cs`**

Тело существующего `GrantAccess` (строки 19–56) переезжает в приватный метод

```csharp
private Task GrantAccessCore(GrantAccessRequest request, ProjectActiveRequirement activeRequirement, string operationName)
```

— тело без изменений, кроме: `ProjectActiveRequirement.AllowBlocked` в вызове `ChangeProjectProperties` заменяется на параметр `activeRequirement`, и в тот же вызов явно добавляется `operationName: operationName` (последний аргумент).

Публичный `GrantAccess` становится:

```csharp
public Task GrantAccess(GrantAccessRequest request)
    => GrantAccessCore(request, ProjectActiveRequirement.AllowBlocked, nameof(GrantAccess));
```

`GrantFullAccess` — вызов `GrantAccessCore(request, ProjectActiveRequirement.AllowArchivedOrBlocked, nameof(GrantFullAccess))`; объект `GrantAccessRequest` формируется как сейчас (строки 232–240), только передаётся в ядро. Комментарий про ADR023 у `AllowBlocked` в публичном `GrantAccess` сохранить.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test src/JoinRpg.Services.Impl.Test --filter "FullyQualifiedName~ProjectAccessServiceTest"`
Expected: PASS все тесты класса, включая новый и неизменные `GrantAccess_ArchivedProject_Throws`, `GrantAccess_BlockedProject_Succeeds`, `GrantFullAccess_AdminWithRemovedRow_ReactivatesSameRow`.

- [ ] **Step 5: Format and commit**

```bash
dotnet format
git add src/JoinRpg.Services.Impl/Projects/Metadata/ProjectAccessService.cs src/JoinRpg.Services.Impl.Test/Projects/ProjectAccessServiceTest.cs docs/superpowers/plans/2026-11-03-grant-access-archived.md docs/superpowers/specs/2026-11-03-grant-access-archived-design.md
git commit -m "feat: доступ техподдержки в архивные проекты"
```

### Task 2: Правка ADR023

**Files:**
- Modify: `docs/adr023-project-blocked-status.md` (таблица требований, строка с `AllowBlocked`, ~строка 43)

**Interfaces:**
- Consumes: ничего из кода.
- Produces: текстовая фиксация исключения для `GrantFullAccess` (пригодится при ревью ADR субагентом).

- [ ] **Step 1: Поправить таблицу требований**

В строке `AllowBlocked` таблицы пример «выдача и правка прав мастеров» уточнить: выдача прав мастерам (`GrantAccess`) и их правка — `AllowBlocked`; исключение — `GrantFullAccess` админа сайта («Поставить себе доступ»), он `AllowArchivedOrBlocked`, потому что техподдержке нужен доступ в архивные проекты. Одна-две фразы, стиль ADR.

- [ ] **Step 2: Commit**

```bash
git add docs/adr023-project-blocked-status.md
git commit -m "docs(adr023): GrantFullAccess допустим в архивных проектах"
```
