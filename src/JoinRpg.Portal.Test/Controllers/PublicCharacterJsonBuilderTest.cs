using System.Text.Json;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.Portal.Controllers;
using JoinRpg.Services.Interfaces;
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

    private static JsonElement BuildJson(UserLinkViewModel playerLink)
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
                IsAvailable: false),
            Description = (JoinHtmlString)new MarkupString("Описание роли"),
            PlayerLink = playerLink,
            ActiveClaimsCount = 1,
        };

        var result = new PublicCharacterJsonBuilder(
            new FakeUriService(),
            new FakeUserLinkLocator(),
            _ => "https://example.com/claim")
            .Build(character);

        // Те же опции, что и в контроллере: имена свойств в PascalCase.
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = null });
        return JsonDocument.Parse(json).RootElement;
    }

    private sealed class FakeUriService : IUriService
    {
        public string Get(ILinkable link) => GetUri(link).AbsoluteUri;

        public Uri GetUri(ILinkable link) => new($"https://example.com/{link.ProjectId}/{link.Identification}");
    }

    private sealed class FakeUserLinkLocator : IUriLocator<UserLinkViewModel>
    {
        public Uri GetUri(UserLinkViewModel target) => new($"https://example.com/user/{target.UserId?.Value}");
    }
}
