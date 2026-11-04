# BrokenCharactersFilter в проблемах заявок — план реализации

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `BrokenCharactersFilter` считает структурные Fatal-проблемы (`NoParentGroup`, `GroupIsBroken`) и для заявок, чтобы выполнялся инвариант «проблемы утверждённой заявки — надмножество проблем персонажа».

**Architecture:** Фильтр реализует оба интерфейса — `ICharacterProblemFilter` и `IClaimProblemFilter`; заявочная сторона делегирует в ту же логику через `ClaimInfo.Character`. Регистрация в `JoinRpgDomainModule` — assembly-scanning (`AssignableTo`/`AsClosedTypesOf`), отдельной регистрации не требуется. Для всех заявок, не только утверждённых; флаги не меняются (`BlocksCheckIn` остаётся `false`).

**Tech Stack:** C#, .NET, xUnit + Shouldly + `MockedProject` (JoinRpg.DataModel.Mocks), Autofac.

**Spec:** `docs/superpowers/specs/2026-11-04-broken-characters-in-claim-problems-design.md`

## Global Constraints

- Править `.cs` только через редактор (Edit/Write): в дереве CRLF, потоковые правки ставят LF и ломают `dotnet format --verify-no-changes`.
- Без regex.
- Перед коммитом: `dotnet format` (применить стиль), потом `dotnet format --verify-no-changes --severity warn`.
- Коммит: `git commit -m "..."`, без `--edit`.
- Локально не гонять интеграционные тесты — только `src/JoinRpg.Domain.Test`.

## Review Focus

- **Персонаж вообще без групп** (`NoParentGroup`): у заявки на нём тоже должна появиться эта Fatal-проблема. → Task 1, Step 1, тест `ClaimOnCharacterWithoutGroupSeesNoParentGroup`.
- **Неутверждённая заявка** тоже видит структурные проблемы (принятое решение «для всех заявок»): тест `ClaimOnCharacterInBrokenGroupSeesGroupIsBroken` собирает контекст через `Mock.CreateClaim` (статус по умолчанию не Approved). → Task 1, Step 1.
- **Полевые проблемы в инварианте**: надмножество проверяется не только на структурных проблемах — в тесте инварианта добавлено обязательное claim-слойное поле, так что персонажная сторона видит и `FieldIsEmpty`. → Task 1, Step 1.
- **Severity-порог одинаково работает на обеих сторонах** — существующий тест `ValidateCombinesFilterAndFieldProblemsAndFiltersBySeverity` (сторона персонажа) и `ClaimOnCharacterInBrokenGroupSeesGroupIsBroken` (Fatal проходит дефолтный порог Hint) это покрывают; отдельный тест не нужен.

---

### Task 1: BrokenCharactersFilter на стороне заявок + тест инварианта

**Files:**
- Modify: `src/JoinRpg.Domain/Problems/CharacterProblemFilters/BrokenCharactersFilter.cs`
- Test: `src/JoinRpg.Domain.Test/Problems/BrokenCharactersFilterTest.cs`
- Modify: `src/JoinRpg.Domain/JoinRpgDomainModule.cs` — не требуется (assembly-scanning подхватит фильтр по новому интерфейсу автоматически)

**Interfaces:**
- Consumes: `IClaimProblemFilter.GetProblems(ClaimInfo)` (`src/JoinRpg.Domain/Problems/IClaimProblemFilter.cs:16`), `ClaimInfo.Character` (свойство персонажа-агрегата).
- Produces: `BrokenCharactersFilter : ICharacterProblemFilter, IClaimProblemFilter` — второй метод `IEnumerable<ClaimProblem> GetProblems(ClaimInfo context)`, семантика идентична персонажной стороне.

- [ ] **Step 1: Написать падающие тесты**

В `BrokenCharactersFilterTest.cs` добавить using'и `JoinRpg.DataModel`, `JoinRpg.Domain.Problems`, `JoinRpg.Domain.Problems.CommonProblemFilters`, `JoinRpg.DomainTypes.Characters.Claims` и три теста:

```csharp
[Fact]
public void ClaimOnCharacterInBrokenGroupSeesGroupIsBroken()
{
    // CreateClaim — заявка не утверждена: структурные проблемы видны всем заявкам.
    var character = mock.CreateCharacter("В битой группе");
    character.ParentCharacterGroupIds = [mock.Group.CharacterGroupId];
    _ = mock.CreateClaim(character, mock.Player);

    var problems = ((IClaimProblemFilter)filter).GetProblems(MakeClaimContext(character)).ToArray();

    problems.ShouldHaveSingleItem();
    problems[0].ProblemType.ShouldBe(ClaimProblemType.GroupIsBroken);
    problems[0].Severity.ShouldBe(ProblemSeverity.Fatal);
    problems[0].ExtraInfo.ShouldBe(mock.Group.CharacterGroupName);
}

[Fact]
public void ClaimOnCharacterWithoutGroupSeesNoParentGroup()
{
    var character = mock.CreateCharacter("Бездомный");
    character.ParentCharacterGroupIds = [];
    _ = mock.CreateClaim(character, mock.Player);

    var problems = ((IClaimProblemFilter)filter).GetProblems(MakeClaimContext(character)).ToArray();

    problems.ShouldHaveSingleItem();
    problems[0].ProblemType.ShouldBe(ClaimProblemType.NoParentGroup);
    problems[0].Severity.ShouldBe(ProblemSeverity.Fatal);
}

/// <summary>
/// Инвариант: проблемы утверждённой заявки — надмножество проблем персонажа. Проверяем на
/// сломанной группе и обязательном поле слоя заявки: персонажная сторона видит обе.
/// </summary>
[Fact]
public void ApprovedClaimProblemsAreSupersetOfCharacterProblems()
{
    var character = mock.CreateCharacter("В битой группе");
    character.ParentCharacterGroupIds = [mock.Group.CharacterGroupId];
    _ = mock.CreateApprovedClaim(character, mock.Player);
    _ = mock.AddField(f =>
    {
        f.FieldType = ProjectFieldType.String;
        f.FieldBoundTo = FieldBoundTo.Claim;
        f.MandatoryStatus = MandatoryStatus.Required;
        f.ValidForNpc = true;
    });

    var characterInfo = mock.GetCharacterInfo(character);

    var characterProblems = characterValidator.Validate(characterInfo)
        .Select(p => p.ProblemType).ToArray();
    var claim = characterInfo.Claims.Single(c => c.IsApproved);
    var claimProblems = claimValidator.Validate(new ClaimInfo(new ClaimInCharacter(characterInfo, claim), mock.PlayerInfo))
        .Select(p => p.ProblemType).ToArray();

    characterProblems.ShouldBe([ClaimProblemType.GroupIsBroken, ClaimProblemType.FieldIsEmpty], ignoreOrder: true);
    characterProblems.ShouldAllBe(type => claimProblems.Contains(type));
}
```

и приватные хелперы/поля класса (рядом с существующими):

```csharp
private readonly CharacterProblemValidator characterValidator = new(
    [new BrokenCharactersFilter()],
    [new FieldNotSetFilter(), new InActiveVariantsFilter()]);

private readonly IClaimProblemValidator claimValidator = new ClaimProblemValidator(
    [new BrokenCharactersFilter()],
    [new FieldNotSetFilter(), new InActiveVariantsFilter()]);

private static ClaimInfo MakeClaimContext(Character character)
{
    var characterInfo = mock.GetCharacterInfo(character);
    return new ClaimInfo(new ClaimInCharacter(characterInfo, characterInfo.Claims.Single()), mock.PlayerInfo);
}
```

(хелпер `MakeClaimContext` использует поле `mock` — если компилятор ругается на static, убрать `static`. В тесте инварианта `characterProblems.ShouldBe(...)` — страж от вакуусного надмножества: персонаж обязан иметь ровно эти две проблемы.)

- [ ] **Step 2: Убедиться, что тесты не проходят**

Run: `dotnet test src/JoinRpg.Domain.Test --filter "FullyQualifiedName~BrokenCharactersFilterTest"`
Expected: FAIL — `BrokenCharactersFilter` не реализует `IClaimProblemFilter` (ошибка компиляции теста).

- [ ] **Step 3: Реализовать заявочную сторону фильтра**

В `BrokenCharactersFilter.cs`:
- добавить `using JoinRpg.DomainTypes.Characters.Claims;`;
- класс: `internal class BrokenCharactersFilter : ICharacterProblemFilter, IClaimProblemFilter`;
- добавить метод `public IEnumerable<ClaimProblem> GetProblems(ClaimInfo context) => GetProblems(context.Character);` — делегирует в существующую логику по `CharacterInfo`, без копирования;
- обновить XML-комментарий класса: фильтр работает на обеих сторонах (заявки + персонажи), чтобы проблемы утверждённой заявки были надмножеством проблем персонажа; флаги у типов не заявочные — `BlocksCheckIn` = false, игроку не видны (см. `ClaimProblemVisibleToPlayerTest`).

- [ ] **Step 4: Убедиться, что тесты проходят**

Run: `dotnet test src/JoinRpg.Domain.Test`
Expected: PASS по всему проекту (юнит-тесты, интеграционные не затронуты).

- [ ] **Step 5: Форматирование и финальная проверка**

Run: `dotnet format` затем `dotnet format --verify-no-changes --severity warn`
Expected: обе команды завершаются без ошибок; `git status` — изменены только `BrokenCharactersFilter.cs`, `BrokenCharactersFilterTest.cs`, плюс новые `docs/superpowers/*` из этого плана и спеки.

- [ ] **Step 6: Коммит**

```bash
git add src/JoinRpg.Domain/Problems/CharacterProblemFilters/BrokenCharactersFilter.cs \
        src/JoinRpg.Domain.Test/Problems/BrokenCharactersFilterTest.cs \
        docs/superpowers/specs/2026-11-04-broken-characters-in-claim-problems-design.md \
        docs/superpowers/plans/2026-11-04-broken-characters-in-claim-problems.md
git commit -m "feat: BrokenCharactersFilter считает структурные проблемы и для заявок

Проблемы утверждённой заявки — надмножество проблем персонажа: NoParentGroup
и GroupIsBroken теперь видны и в валидации заявки. BlocksCheckIn не выставляем —
сломанная структура групп не останавливает регистрацию."
```
