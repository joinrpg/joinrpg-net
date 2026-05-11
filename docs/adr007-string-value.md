## Новый генерируемый тип [TypedStringValue]

TypedEntityIdGenerator очень хорошо себя показал. Для замены оставшихся использований SingleValueType<string> надо разработать новый генератор.

### Пример использования

```csharp

[TypedStringValue(MinLength=5, MaxLength=50, Trim=true)]
public record ProjectName(string value);
```

### Генерирует

1. Реализацию ISpanParsable<ProjectName>, которая парсит как из «строка», так и из «ProjectName(строка)»
2. Реализацию IEquatable<string>
3. статический метод `ProjectName? FromOptional(string? value)`
4. Свойство Value `... public string Value {get; } = ValidateAndCanonicalize(value)`
5. Статический метод `ValidateAndCanonicalize`, который проверяет максимальную и минимальную длину, применяет операцию Trim при необходимости, а также вызывает метод `CustomValidateAndCanonicalize()` если он не определен.
6. Параметры имеют следующие дефолтные значения: MinLength=1, MaxLength=999, Trim=true
7. Кастомный `ToString()`, который возвращает `ProjectName(строка)`