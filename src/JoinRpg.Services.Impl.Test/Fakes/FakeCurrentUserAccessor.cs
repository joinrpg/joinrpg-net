using JoinRpg.Interfaces;

namespace JoinRpg.Services.Impl.Test.Fakes;

internal sealed class FakeCurrentUserAccessor(int userId, bool isAdmin = false) : ICurrentUserAccessor
{
    public int? UserIdOrDefault => userId;
    public UserDisplayName DisplayName => new("Test", null);
    public bool IsAdmin => isAdmin;
    public AvatarIdentification? Avatar => null;
}
