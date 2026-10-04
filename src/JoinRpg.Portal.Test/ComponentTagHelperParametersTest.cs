using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace JoinRpg.Portal.Test;

/// <summary>
/// Параметры, которые razor-страницы передают островам через тег-хелпер
/// <c>&lt;component type="typeof(X)" param-Y="..." /&gt;</c>, не проверяются ни компилятором,
/// ни анализаторами: имена уезжают строками в словарь и сверяются с типом только в рантайме.
/// <para>
/// Ровно так сломалось редактирование заявки с полем «ведущий»: в шаблон поля попал
/// <c>param-ProjectId</c>, которого у <c>JoinUserLinkEditor</c> никогда не было. Сервер отдавал
/// 200, в логах портала — ничего, а остров падал уже в браузере с
/// <c>InvalidOperationException: does not have a property matching the name 'ProjectId'</c>.
/// </para>
/// </summary>
public class ComponentTagHelperParametersTest
{
    private static List<Type>? componentTypes;

    public static TheoryData<string, string, string> ComponentParameters()
    {
        var data = new TheoryData<string, string, string>();
        foreach (var usage in FindUsages())
        {
            data.Add(usage.View, usage.TypeName, usage.ParameterName);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(ComponentParameters))]
    public void ComponentShouldHaveParameter(string view, string typeName, string parameterName)
    {
        var componentType = ResolveComponentType(typeName);
        componentType.ShouldNotBeNull($"{view}: не найден тип компонента {typeName}");

        var accepted = AcceptedParameterNames(componentType);
        if (accepted is null)
        {
            // Компонент с CaptureUnmatchedValues принимает что угодно — проверять нечего.
            return;
        }

        accepted.Contains(parameterName)
            .ShouldBeTrue(
                $"{view}: компонент {typeName} не принимает параметр {parameterName}. "
                + $"Есть: {string.Join(", ", accepted.Order(StringComparer.Ordinal))}");
    }

    /// <summary>
    /// Если поиск вдруг перестанет что-либо находить (переехали вьюхи, сменился синтаксис
    /// тег-хелпера) — тест выше станет зелёным просто потому, что данных нет.
    /// </summary>
    [Fact]
    public void ShouldFindSomethingToCheck() => FindUsages().ShouldNotBeEmpty();

    /// <summary>
    /// Blazor сверяет имена без учёта регистра.
    /// </summary>
    /// <returns>
    /// <c>null</c>, если компонент объявил <c>CaptureUnmatchedValues</c> и принимает что угодно.
    /// </returns>
    private static HashSet<string>? AcceptedParameterNames(Type componentType)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in componentType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            var parameter = property.GetCustomAttribute<ParameterAttribute>();
            if (parameter is null && property.GetCustomAttribute<CascadingParameterAttribute>() is null)
            {
                continue;
            }

            if (parameter?.CaptureUnmatchedValues == true)
            {
                return null;
            }

            _ = names.Add(property.Name);
        }
        return names;
    }

    private static Type? ResolveComponentType(string typeName)
        => ComponentTypes().FirstOrDefault(t => t.FullName == typeName)
        ?? ComponentTypes().FirstOrDefault(t => t.Name == typeName);

    /// <summary>
    /// Portal.Test тянет за собой Portal → Blazor.Client → все сборки с компонентами, поэтому
    /// достаточно пройтись по выходному каталогу (см. <see cref="IslandModelSingletonsJsonTest"/>).
    /// </summary>
    private static List<Type> ComponentTypes()
        => componentTypes ??= [.. Directory.EnumerateFiles(AppContext.BaseDirectory, "JoinRpg.*.dll")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null)
            .Select(name => Assembly.Load(new AssemblyName(name!)))
            .SelectMany(SafeGetTypes)
            .Where(type => type is { IsAbstract: false } && type.IsAssignableTo(typeof(IComponent)))];

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type is not null)!;
        }
    }

    private static List<(string View, string TypeName, string ParameterName)> FindUsages()
    {
        var result = new List<(string, string, string)>();
        var views = Path.Combine(RepoRoot(), "src", "JoinRpg.Portal");
        foreach (var file in Directory.EnumerateFiles(views, "*.cshtml", SearchOption.AllDirectories))
        {
            var view = Path.GetRelativePath(views, file);
            foreach (var tag in ComponentTags(File.ReadAllText(file)))
            {
                if (TypeNameOf(tag) is not { } typeName)
                {
                    continue;
                }

                foreach (var parameterName in ParameterNamesOf(tag))
                {
                    result.Add((view, typeName, parameterName));
                }
            }
        }
        return result;
    }

    private static IEnumerable<string> ComponentTags(string text)
    {
        var position = 0;
        while (true)
        {
            var start = text.IndexOf("<component", position, StringComparison.Ordinal);
            if (start < 0)
            {
                yield break;
            }

            // Тег-хелпер всегда самозакрывающийся; «/>» внутри значений параметров не встречается,
            // а «>» — встречается (дженерики в C#-выражениях), поэтому границу ищем именно по «/>».
            var end = text.IndexOf("/>", start, StringComparison.Ordinal);
            if (end < 0)
            {
                yield break;
            }

            yield return text[start..end];
            position = end + 2;
        }
    }

    private static string? TypeNameOf(string tag)
    {
        const string marker = "typeof(";
        var start = tag.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }

        start += marker.Length;
        var end = tag.IndexOf(')', start);
        return end < 0 ? null : tag[start..end].Trim();
    }

    private static IEnumerable<string> ParameterNamesOf(string tag)
    {
        const string marker = "param-";
        var position = 0;
        while (true)
        {
            var start = tag.IndexOf(marker, position, StringComparison.Ordinal);
            if (start < 0)
            {
                yield break;
            }

            start += marker.Length;
            var end = tag.IndexOf('=', start);
            if (end < 0)
            {
                yield break;
            }

            yield return tag[start..end].Trim();
            position = end;
        }
    }

    /// <summary>
    /// Вьюхи — это контент, в выходной каталог теста они не копируются, поэтому ищем их
    /// в исходниках: от каталога сборки вверх до файла решения.
    /// </summary>
    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Joinrpg.slnx")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new InvalidOperationException("Не найден корень репозитория (Joinrpg.slnx)");
    }
}
