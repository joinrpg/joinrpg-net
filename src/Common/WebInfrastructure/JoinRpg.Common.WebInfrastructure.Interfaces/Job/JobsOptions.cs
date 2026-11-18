namespace JoinRpg.Common.WebInfrastructure.Job;

/// <summary>
/// Настройки фоновых джоб (секция <c>DailyJobs</c>).
/// </summary>
public class JobsOptions
{
    /// <summary>
    /// Имя секции конфигурации (исторически <c>DailyJobs</c>).
    /// </summary>
    public const string SectionName = "DailyJobs";

    /// <summary>
    /// Выключенные джобы: короткое (<c>AdvertisementJob</c>) или полное
    /// (<c>JoinRpg.Services.Advertisement.AdvertisementJob</c>) имя класса, без учёта регистра.
    /// Выключенная джоба не запускается по расписанию и не запускается вручную со страницы джоб.
    /// </summary>
    public string[] Disabled { get; set; } = [];

    /// <summary>
    /// Выключена ли джоба с этим типом (совпадение по короткому или полному имени класса, без учёта регистра).
    /// </summary>
    public bool IsDisabled(Type jobType) => Disabled.Any(name => Matches(name, jobType));

    /// <summary>
    /// Имена из <see cref="Disabled"/>, которые не соответствуют ни одной известной джобе.
    /// </summary>
    public IReadOnlyCollection<string> GetUnknownNames(IEnumerable<Type> knownJobTypes)
    {
        var known = knownJobTypes.ToList();
        return [.. Disabled.Where(name => !known.Any(jobType => Matches(name, jobType)))];
    }

    private static bool Matches(string name, Type jobType)
    {
        var trimmed = name.Trim();
        return string.Equals(trimmed, jobType.Name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, jobType.FullName, StringComparison.OrdinalIgnoreCase);
    }
}
