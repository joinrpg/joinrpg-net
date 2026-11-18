using System.Reflection;

namespace JoinRpg.Portal.Test;

/// <summary>
/// Готовые статические экземпляры моделей («sentinel»-константы вроде
/// <c>UserLinkViewModel.Hidden</c>) — отдельный источник бед: их собирают руками, мимо билдеров,
/// и ни один тест билдера их не трогает. При этом они точно так же уезжают в Blazor-остров по JSON.
/// <para>
/// Ровно на такой константе прод сломался: <c>UserLinkViewModel.Hidden</c> содержал
/// <c>UserIdentification(-1)</c>, сервер отдавал валидный JSON и 200, а остров ProjectRoleGrid
/// падал на десериализации и вис на «Идет загрузка...» (см. <see cref="JsonRoundTrip"/>).
/// </para>
/// </summary>
public class IslandModelSingletonsJsonTest
{
    /// <summary>
    /// Сборки, чьи типы ездят между сервером и островами.
    /// <para>
    /// Portal.Test тянет за собой Portal → Blazor.Client → все контрактные сборки, поэтому
    /// достаточно пройтись по выходному каталогу. Список ссылок сборки брать нельзя: компилятор
    /// выкидывает из него всё, на типы чего нет прямых обращений в коде.
    /// </para>
    /// </summary>
    private static List<Assembly> ContractAssemblies()
        => [.. Directory.EnumerateFiles(AppContext.BaseDirectory, "JoinRpg.*.dll")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null && IsContractAssemblyName(name))
            .Select(name => Assembly.Load(new AssemblyName(name!)))];

    private static bool IsContractAssemblyName(string name)
        => name.StartsWith("JoinRpg.Web.", StringComparison.Ordinal)
        || name is "JoinRpg.Common.WebComponents" or "JoinRpg.CommonUI.Models";

    public static TheoryData<string> Singletons()
    {
        var data = new TheoryData<string>();
        foreach (var name in FindSingletons().Select(s => s.Name).Order(StringComparer.Ordinal))
        {
            data.Add(name);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Singletons))]
    public void ShouldSurviveJsonRoundTrip(string name)
    {
        var singleton = FindSingletons().Single(s => s.Name == name);
        _ = JsonRoundTrip.Ensure(singleton.Value, singleton.Type);
    }

    /// <summary>
    /// Если тест выше вдруг перестанет находить хоть что-нибудь (переехали сборки, переименовали
    /// проекты) — он станет зелёным просто потому, что данных нет. Эта проверка ловит такое.
    /// </summary>
    [Fact]
    public void ShouldFindSomethingToCheck() => FindSingletons().ShouldNotBeEmpty();

    private static List<(string Name, Type Type, object Value)> FindSingletons()
    {
        var result = new List<(string Name, Type Type, object Value)>();
        foreach (var assembly in ContractAssemblies())
        {
            foreach (var type in assembly.GetExportedTypes().Where(IsModelType))
            {
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (IsModelType(field.FieldType) && field.GetValue(null) is { } value)
                    {
                        result.Add(($"{type.Name}.{field.Name}", field.FieldType, value));
                    }
                }

                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
                {
                    if (IsModelType(property.PropertyType)
                        && property.GetMethod is not null
                        && property.GetIndexParameters().Length == 0
                        && property.GetValue(null) is { } value)
                    {
                        result.Add(($"{type.Name}.{property.Name}", property.PropertyType, value));
                    }
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Модель — это обычный тип-данные из контрактных сборок: не делегат, не абстракция,
    /// не строка. Типы из других сборок (BCL, домен) проверяют свои тесты.
    /// </summary>
    private static bool IsModelType(Type type)
        => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
        && !type.IsAssignableTo(typeof(Delegate))
        && type != typeof(string)
        && type.Assembly.GetName().Name is { } name
        && IsContractAssemblyName(name);
}
