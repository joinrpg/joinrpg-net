using System.Text.Json;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.Portal.Controllers;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.CommonTypes;
using JoinRpg.Web.ProjectCommon;
using Microsoft.AspNetCore.Components;

namespace JoinRpg.Portal.Test.Controllers;

/// <summary>
/// Публичный JSON ролей читают анонимы (виджет встраивания на сайтах игр), поэтому проверяем не
/// модель, а именно то, что уходит в ответ.
/// </summary>
public class PublicCharacterJsonBuilderTest
{
    private static readonly UserInfoHeader Player =
        new(new UserIdentification(42), new UserDisplayName("Василий Пупкин", "Василий Пупкин"));

    [Fact]
    public void VisiblePlayerIsSerialized()
    {
        var json = BuildJson(new UserLinkViewModel(Player));

        json.GetProperty("PlayerName").GetString().ShouldBe("Василий Пупкин");
        // Формат типизированного id менять нельзя — его читают внешние сайты игр.
        json.GetProperty("PlayerId").GetString().ShouldBe(Player.UserId.ToString());
        json.GetProperty("PlayerLink").GetString().ShouldBe("https://example.com/user/42");
    }

    /// <summary>
    /// Ссылки строятся локаторами и всегда абсолютные — по ним ходят внешние сайты игр.
    /// </summary>
    [Fact]
    public void LinksAreAbsoluteAndBuiltByLocators()
    {
        var json = BuildJson(new UserLinkViewModel(Player), isAvailable: true);

        json.GetProperty("CharacterLink").GetString().ShouldBe("https://example.com/7/character/13");
        json.GetProperty("ClaimLink").GetString().ShouldBe("https://example.com/7/claim/add/13");
    }

    /// <summary>
    /// На занятую роль заявиться нельзя — ссылки «заявиться» в ответе быть не должно.
    /// </summary>
    [Fact]
    public void UnavailableCharacterHasNoClaimLink()
    {
        var json = BuildJson(new UserLinkViewModel(Player));

        json.GetProperty("ClaimLink").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    /// <summary>
    /// Так ссылку строит конвейер: в режиме Hide данные игрока не попадают уже в модель.
    /// </summary>
    [Fact]
    public void HiddenPlayerLeaksNothing()
    {
        var json = BuildJson(new UserLinkViewModel(Player, ViewMode.Hide));

        AssertNoPlayerData(json);
    }

    /// <summary>
    /// Второй рубеж: даже если в модель каким-то образом попали реальные данные, режим Hide всё
    /// равно обязан вырезать их из ответа (issue #4895 — раньше вырезалась только ссылка).
    /// </summary>
    [Fact]
    public void HiddenPlayerLeaksNothingEvenIfModelStillCarriesData()
    {
        var json = BuildJson(new UserLinkViewModel(Player.UserId, Player.DisplayName.DisplayName, ViewMode.Hide));

        AssertNoPlayerData(json);
    }

    private static void AssertNoPlayerData(JsonElement json)
    {
        json.GetProperty("PlayerName").ValueKind.ShouldBe(JsonValueKind.Null);
        json.GetProperty("PlayerId").ValueKind.ShouldBe(JsonValueKind.Null);
        json.GetProperty("PlayerLink").ValueKind.ShouldBe(JsonValueKind.Null);

        // Идентификатор игрока не должен просочиться и в соседние поля (например, в ссылку).
        json.GetRawText().ShouldNotContain("42");
    }

    private static JsonElement BuildJson(UserLinkViewModel playerLink, bool isAvailable = false)
    {
        var characterId = new CharacterIdentification(7, 13);
        var character = new CharacterViewModel
        {
            CharacterId = characterId.CharacterId,
            ProjectId = characterId.ProjectId.Value,
            CharacterName = "Боромир",
            IsFirstCopy = true,
            ApplyStatus = new CharacterApplyViewModel(
                characterId,
                CharacterBusyStatusView.HasPlayer,
                SlotCount: null,
                IsHot: false,
                isAvailable),
            Description = (JoinHtmlString)new MarkupString("Описание роли"),
            PlayerLink = playerLink,
            ActiveClaimsCount = 1,
        };

        var locator = new FakeUriLocator();
        var result = new PublicCharacterJsonBuilder(locator, locator, locator).Build(character);

        // Те же опции, что и в контроллере: имена свойств в PascalCase.
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = null });
        return JsonDocument.Parse(json).RootElement;
    }

    /// <summary>
    /// Все локаторы в портале реализует один <c>UriServiceImpl</c>, поэтому и фейк общий.
    /// </summary>
    private sealed class FakeUriLocator :
        IUriLocator<CharacterIdentification>,
        ICharacterUriLocator,
        IUriLocator<UserLinkViewModel>
    {
        public Uri GetUri(CharacterIdentification target) => GetDetailsUri(target);

        public Uri GetDetailsUri(CharacterIdentification characterId) =>
            new($"https://example.com/{characterId.ProjectId.Value}/character/{characterId.CharacterId}");

        public Uri GetAddClaimUri(CharacterIdentification characterId) =>
            new($"https://example.com/{characterId.ProjectId.Value}/claim/add/{characterId.CharacterId}");

        public Uri GetEditUri(CharacterIdentification characterId) => throw new NotSupportedException();

        public Uri GetUri(UserLinkViewModel target) => new($"https://example.com/user/{target.UserId?.Value}");
    }
}
