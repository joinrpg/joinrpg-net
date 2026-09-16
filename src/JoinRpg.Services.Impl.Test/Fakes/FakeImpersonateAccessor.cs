using JoinRpg.Interfaces;

namespace JoinRpg.Services.Impl.Test.Fakes;

/// <summary>
/// Подмена пользователя в тестах. Не только записывает вызовы, но и действительно подменяет
/// текущего пользователя во всех зарегистрированных <see cref="FakeCurrentUserAccessor"/>: иначе
/// автоприём утверждал бы заявку от имени игрока и упирался бы в проверку прав.
/// </summary>
/// <remarks>
/// Админскость подменённого пользователя не моделируется: у claim-операций admin-bypass'а нет и не
/// должно быть (ADR014), так что читать её некому.
/// </remarks>
internal sealed class FakeImpersonateAccessor : IImpersonateAccessor
{
    private readonly List<FakeCurrentUserAccessor> accessors = [];
    private readonly List<UserIdentification> saved = [];

    /// <summary>Пользователи, под которых подменялись, в порядке вызовов.</summary>
    public List<UserIdentification> Impersonated { get; } = [];

    /// <summary>Сколько раз подмену снимали. Должно совпадать с числом входов — <c>finally</c>.</summary>
    public int StopCount { get; private set; }

    /// <summary>
    /// Регистрирует фейк текущего пользователя, на который подмена должна действовать. Их у одного
    /// сервиса несколько — свой у props-сервиса, свой у <c>CommentHelper</c>.
    /// </summary>
    public FakeCurrentUserAccessor Track(FakeCurrentUserAccessor accessor)
    {
        accessors.Add(accessor);
        return accessor;
    }

    public void StartImpersonate(UserIdentification userId, UserDisplayName displayName, bool IsAdmin)
    {
        Impersonated.Add(userId);
        saved.Clear();
        saved.AddRange(accessors.Select(a => a.UserIdentification));

        foreach (var accessor in accessors)
        {
            accessor.UserIdentification = userId;
        }
    }

    public void StopImpersonate()
    {
        StopCount++;

        foreach (var (accessor, previous) in accessors.Zip(saved))
        {
            accessor.UserIdentification = previous;
        }

        saved.Clear();
    }
}
