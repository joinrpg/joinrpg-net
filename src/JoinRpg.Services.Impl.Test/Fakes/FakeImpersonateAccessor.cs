using JoinRpg.Interfaces;

namespace JoinRpg.Services.Impl.Test.Fakes;

/// <summary>
/// Записывает подмену пользователя вместо реальной. Нужен автоприёму, который утверждает заявку от
/// имени ответственного мастера.
/// </summary>
internal sealed class FakeImpersonateAccessor : IImpersonateAccessor
{
    /// <summary>Пользователи, под которых подменялись, в порядке вызовов.</summary>
    public List<UserIdentification> Impersonated { get; } = [];

    /// <summary>Сколько раз подмену снимали. Должно совпадать с числом входов — <c>finally</c>.</summary>
    public int StopCount { get; private set; }

    public void StartImpersonate(UserIdentification userId, UserDisplayName displayName, bool IsAdmin)
        => Impersonated.Add(userId);

    public void StopImpersonate() => StopCount++;
}
