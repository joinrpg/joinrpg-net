# LINQ-запросы и LinqKit

## Зачем LinqKit

Entity Framework транслирует LINQ-выражения (`Expression<Func<...>>`) в SQL.
Обычные методы C# (`Func<...>`) — **не транслируются**: они выполняются в памяти после загрузки всех данных.

LinqKit позволяет **переиспользовать** Expression-предикаты и проекции в EF-запросах:
- `AsExpandable()` — расширяет `IQueryable` для поддержки `Invoke`
- `.Invoke(args...)` — вызывает Expression внутри другого Expression; LinqKit встраивает тело предиката в запрос

## Шаблон использования

### 1. Определить предикат как Expression

```csharp
internal static class MyPredicate
{
    public static Expression<Func<MyEntity, bool>> GetPredicate(DateTime now)
    {
        return entity => entity.Active && entity.EndDate >= now;
    }
}
```

Если нужно передать несколько параметров (например, сущность + вычисленное значение из join):

```csharp
public static Expression<Func<Project, DateTime, bool>> GetPredicate(DateTime now)
{
    var threshold = now.AddDays(-60);
    return (project, lastUpdated) =>
        project.Active && lastUpdated > threshold;
}
```

### 2. Использовать предикат в EF-запросе

```csharp
// Обязательно: AsExpandable() на IQueryable
private IQueryable<Project> AllProjects => Ctx.ProjectsSet.AsExpandable();

private async Task<List<MyDto>> GetFiltered()
{
    var predicate = MyPredicate.GetPredicate(DateTime.Now);

    var query = from project in AllProjects
                join update in GetLastUpdateQuery() on project.ProjectId equals update.ProjectId
                where predicate.Invoke(project, update.LastUpdated)  // Invoke = встраивание в SQL
                select new MyDto { ... };

    return await query.ToListAsync();
}
```

### 3. Тестировать предикат напрямую

```csharp
[Fact]
public void ActiveProject_ShouldMatch()
{
    var project = new Project { Active = true, ... };
    var result = MyPredicate.GetPredicate(Now).Compile()(project, RecentlyUpdated);
    result.ShouldBeTrue();
}
```

`.Compile()` превращает Expression в обычный делегат для выполнения в тестах (без EF).

## Правила

- Все предикаты и проекции для EF-запросов — только `Expression<Func<...>>`, **не** `Func<...>`
- Если метод возвращает `bool` (не Expression) — он выполняется **в памяти**, EF его не видит
- `AsExpandable()` ставится один раз на `IQueryable` (обычно в свойстве репозитория)
- LinqKit подключён в `JoinRpg.Dal.Impl` как `LinqKit.EntityFramework`

## Примеры в коде

- `KogdaIgraMissingGamesPredicate` — предикат с двумя параметрами (Project + DateTime)
- `ProjectPredicates`, `ClaimPredicates`, `CharacterPredicates` — предикаты по сущностям
- `ProjectRepository.GetLastUpdateQuery` — проекция с `Expression<Func<T, DateTime>>`
