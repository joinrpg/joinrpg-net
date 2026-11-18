# JoinRpg.Common.PrimitiveTypes.SourceGenerator

Генерирует код для своих типизированных Id и строковых значений: пишете `partial record` с
атрибутом — получаете разбор, `ToString`, сравнение; для Id — ещё JSON-конвертер и `TypeConverter`,
для строк — проверку длины. Для готовых типов (`UserIdentification`, `Email`) он не нужен — хватит
`JoinRpg.Common.PrimitiveTypes`.

```
dotnet add package JoinRpg.Common.PrimitiveTypes
dotnet add package JoinRpg.Common.PrimitiveTypes.SourceGenerator
```

У генератора в `.csproj` допишите `PrivateAssets="all"`. Версии обоих пакетов держите
одинаковыми: они выходят вместе, и сгенерированный код зовёт хелперы из PrimitiveTypes той же
версии. Нужен net10.0.

## `[TypedEntityId]`

```csharp
[TypedEntityId]
public partial record GameIdentification(int Value);

[TypedEntityId]
public partial record GamePlayerIdentification(GameIdentification GameId, int PlayerId);
```

Генерируется:

- `ToString()` — `GameId(5)`, для составного — `GamePlayerId(5-12)`;
- `ISpanParsable<T>`: `Parse`/`TryParse` принимают `GameId(5)`, `Game(5)`, `GameIdentification(5)`
  и просто `5`; числа составного Id разделяются `-` или `,`;
- `Id` — последнее число;
- `IComparable<T>` — сравнивает только последнее число (у составного `GameId` не учитывается);
  не генерируется, если `CompareTo` написан руками;
- `FromOptional(...)` с nullable-параметрами — `null` на входе даёт `null`;
- `[JsonConverter]` — в JSON Id пишется строкой, тем же форматом, что `ToString()`; работает и
  для ключей словаря;
- `[TypeConverter]` — Id можно держать в опциях конфигурации;
- для Id из одного числа — неявное приведение к `int`/`long` и `IEntityId<T, TValue>`
  (по нему пакет `JoinRpg.Common.EntityFrameworkCore` маппит колонку EF Core);
- для составного — конструктор из плоских чисел (`new GamePlayerIdentification(5, 12)`) и
  `FromList(IEnumerable<int>, GameIdentification)`; если первый параметр сам составной Id — ещё
  свойства его вложенных Id.

Руками можно написать только `CompareTo`; свои `ToString`, `Parse` и `FromOptional` дадут CS0111.

Знак проверяет только `Parse` и только у Id из одного числа: `Parse("0")` не пройдёт,
`new GameIdentification(0)` пройдёт.

| Параметр | По умолчанию | Зачем |
|---|---|---|
| `ShortName` | для `XxxIdentification` — `XxxId`, иначе имя типа | префикс в `ToString` и `Parse` |
| `AdditionalPrefixes` | — | старые префиксы, которые `Parse` тоже примет |
| `AllowNonPositive` | `false` | разрешить в `Parse` 0 и отрицательные; только для Id из одного `long` |

## `[TypedStringValue]`

```csharp
[TypedStringValue(MaxLength = 100)]
public partial record GameTitle(string Value);
```

Генерируется: константы `MinLength`/`MaxLength`, неявные приведения из `string` и в `string`,
`FromOptional(string?)` (пустая строка или пробелы дают `null`), `Equals(string?)`, сравнение,
`ToString()` вида `GameTitle(...)` (для вывода — `.Value`), `Parse`/`TryParse`.

Значение проверяют приведение из `string`, `FromOptional` и `Parse`: обрезают пробелы и
проверяют длину, при ошибке — `ArgumentException` (`TryParse` возвращает `false`). Конструктор
`new GameTitle(...)` и десериализация из JSON не проверяют ничего. Своего JSON-конвертера у
строк нет: в JSON это объект `{"Value": "..."}`.

| Параметр | По умолчанию | Зачем |
|---|---|---|
| `MinLength` | 1 | минимальная длина после обрезки |
| `MaxLength` | 999 | `int.MaxValue` — без проверки длины |
| `Trim` | `true` | обрезать пробелы по краям |

Своя проверка или нормализация — статический `CustomValidateAndCanonicalize(string) → string` в
том же типе. Вызывается после обрезки и проверки длины, результат становится `Value`. Отказ —
`ArgumentException`: другое исключение `TryParse` пробросит.

```csharp
internal static string CustomValidateAndCanonicalize(string value)
{
    if (!value.Contains('@'))
    {
        throw new ArgumentException("Incorrect email", nameof(value));
    }
    return value;
}
```

Генератор пропускает приведения из `string` и в `string`, если они написаны руками; `Parse` и
`TryParse` — если в типе есть любой статический `Parse` или `TryParse`; `CompareTo` и
`Equals(string?)` — если тип сам объявляет `IComparable<T>` или `IEquatable<string>`. Остальное,
написанное руками, даст CS0111: `ToString`, `FromOptional(string?)`, а также
`CompareTo`/`Equals(string?)` без объявленного интерфейса.

## Когда генератор молчит

Тип — `public partial record` (иначе сразу CS0260 или CS0262). Генератор ничего не создаёт и
ничего не сообщает, если:

- нет позиционных параметров;
- у Id есть параметр не `int`, не `long` и не `[TypedEntityId]`-тип из того же проекта (тип из
  другой сборки, например `UserIdentification` из пакета, не разворачивается);
- у Id нет ни одного параметра `int`/`long` (Id из одних вложенных Id не генерируется);
- у строки не ровно один параметр `string`.

Ошибки тогда появятся дальше: нет `Parse` или приведения из `string`. Кроме того, `long`
допустим только единственным параметром, а чисел в Id — не больше четырёх.

Как устроен и как правится сам генератор — [docs/source-generator.md](https://github.com/joinrpg/joinrpg-net/blob/master/docs/source-generator.md).
