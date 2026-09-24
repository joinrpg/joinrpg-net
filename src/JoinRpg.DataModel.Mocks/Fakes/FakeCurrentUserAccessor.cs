using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Interfaces;

namespace JoinRpg.DataModel.Mocks.Fakes;

/// <summary>
/// Текущий пользователь в тестах — общий для всех тестовых проектов.
/// </summary>
public sealed class FakeCurrentUserAccessor(UserIdentification userId, bool isAdmin = false) : ICurrentUserAccessor
{
    public FakeCurrentUserAccessor(int userId, bool isAdmin = false)
        : this(new UserIdentification(userId), isAdmin)
    {
    }

    /// <summary>Аноним: тесты, которым пользователь не важен, обходятся без него.</summary>
    public FakeCurrentUserAccessor()
        : this(new UserIdentification(0))
    {
    }

    /// <summary>Свойство изменяемое: часть тестов гоняет один и тот же сервис от разных лиц.</summary>
    public UserIdentification UserIdentification { get; set; } = userId;

    public int? UserIdOrDefault => UserIdentification.Value;

    public UserDisplayName DisplayName => new("Test", null);

    public bool IsAdmin => isAdmin;

    public AvatarIdentification? Avatar => null;
}
