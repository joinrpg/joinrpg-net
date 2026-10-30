# JoinRpg.Common.EntityFrameworkCore

Хранит типизированные Id из `JoinRpg.Common.PrimitiveTypes` (и свои, сгенерированные
`[TypedEntityId]`) в колонках EF Core как обычные числа, и регистрирует DbContext
для PostgreSQL (`AddJoinEfCoreDbContext`).

```
dotnet add package JoinRpg.Common.EntityFrameworkCore
```

Нужен net10.0 и EF Core 10.

```csharp
using JoinRpg.Common.EntityFrameworkCore;
using JoinRpg.Common.PrimitiveTypes;

public class MyDbContext(DbContextOptions options) : DbContext(options)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // GameIdentification — свой [TypedEntityId] с одним int
        configurationBuilder.Properties<GameIdentification>()
            .HaveEntityIdValueConversion<GameIdentification, int>();
        configurationBuilder.Properties<UserIdentification>()
            .HaveEntityIdValueConversion<UserIdentification, int>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ... настройка сущностей ...

        // Ключ с конвертером EF сам identity не делает — включаем, как у int-ключа.
        modelBuilder.EntityIdsSetValueGeneratedOnAdd();
    }
}
```

- `HaveEntityIdValueConversion<TId, TValue>()` — вызывать в `ConfigureConventions`. Без
  конвертера EF примет свойство такого типа за навигацию к новой сущности. `TValue` (`int` или
  `long`) указывается явно, вывести его компилятор не может.
- `EntityIdsSetValueGeneratedOnAdd()` — вызывать в конце `OnModelCreating`: сущности, настроенные
  после вызова, он не затронет. Делает identity все ключи из одного типизированного Id, в том
  числе внешние (`UserIdentification` с id.joinrpg.ru) — таким после вызова верните
  `ValueGeneratedNever()`.

Составные Id (`GamePlayerId(5-12)`) конвертера не получают: храните их части отдельными
колонками.

## Регистрация DbContext

`AddJoinEfCoreDbContext` регистрирует DbContext для PostgreSQL: провайдер Npgsql,
EntityFramework.Exceptions (бизнес-исключения вместо `DbUpdateException`), лог SQL в Debug
в dev-режиме и health check `{имя строки подключения}-db` (Degraded при недоступности базы).
Если строки подключения с таким именем нет, ничего не регистрирует и возвращает `false` —
вызывающий решает, критично ли это.

```csharp
using JoinRpg.Common.EntityFrameworkCore;

services.AddJoinEfCoreDbContext<MyDbContext>(
    configuration, environment,
    connectionStringName: "MyDatabase",
    options => options.UseOpenIddict() // опционально
);
```
