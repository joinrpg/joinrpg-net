using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Web.Schedule.Test;

/// <summary>
/// Заглушки локаторов ссылок для bUnit-тестов: библиотеки мокирования (Moq/NSubstitute)
/// в репозитории нет, поэтому пишем руками — как <c>FakeProjectUriLocator</c> в JoinRpg.Web.Claims.Test.
/// </summary>
internal sealed class FakeCharacterUriLocator : ICharacterUriLocator
{
    public Uri GetDetailsUri(CharacterIdentification characterId)
        => new($"https://example.org/{characterId.ProjectId.Value}/character/{characterId.CharacterId}");

    public Uri GetAddClaimUri(CharacterIdentification characterId)
        => new($"https://example.org/{characterId.ProjectId.Value}/character/{characterId.CharacterId}/apply");

    public Uri GetEditUri(CharacterIdentification characterId)
        => new($"https://example.org/{characterId.ProjectId.Value}/character/{characterId.CharacterId}/edit");
}

internal sealed class FakeUserLinkUriLocator : IUriLocator<UserLinkViewModel>
{
    public Uri GetUri(UserLinkViewModel target)
        => new($"https://example.org/user/{target.UserId?.Value}");
}
