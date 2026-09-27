using System.Text.Json;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.WebComponents;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Portal.Controllers;
using JoinRpg.Web.ProjectCommon;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Test.Controllers;

/// <summary>
/// Контрактный тест формы публичного JSON ролей (<c>/{projectId}/roles/{characterGroupId}/indexjson</c>).
/// </summary>
/// <remarks>
/// Эндпоинт помечен <c>[AllowAnonymous]</c> и является публичным API: его читают внешние сайты игр
/// (штатный виджет — <c>wwwroot/external/joinrpg-api.js</c>). Менять типы полей нельзя, а поймать
/// такую поломку по логам невозможно: сервер отдаёт 200 и валидный JSON, ломается потребитель.
/// Так уже случилось: при переезде в этот контроллер (#4859) забыли <c>.Value</c>, и
/// <c>ProjectName</c> из строки превратился в объект <c>{"Value": "..."}</c>.
/// Поэтому проверяем не модель в памяти, а именно текст ответа.
/// </remarks>
public class GameGroupsJsonControllerTest
{
    /// <summary>
    /// Регрессия: <c>ProjectName</c> — строка, а не объект типизированного значения.
    /// </summary>
    [Fact]
    public async Task ProjectNameIsPlainString()
    {
        var json = await GetIndexJson();

        var projectName = json.GetProperty("ProjectName");
        projectName.ValueKind.ShouldBe(JsonValueKind.String);
        projectName.GetString().ShouldBe("Mocked project");
    }

    /// <summary>
    /// Остальные поля конверта ответа — тоже примитивы прежних типов.
    /// </summary>
    [Fact]
    public async Task EnvelopeShapeIsUnchanged()
    {
        var json = await GetIndexJson();

        json.GetProperty("ProjectId").ValueKind.ShouldBe(JsonValueKind.Number);
        json.GetProperty("ShowEditControls").ValueKind.ShouldBe(JsonValueKind.False);
        json.GetProperty("Groups").ValueKind.ShouldBe(JsonValueKind.Array);

        var group = json.GetProperty("Groups").EnumerateArray().First();
        group.GetProperty("CharacterGroupId").ValueKind.ShouldBe(JsonValueKind.Number);
        group.GetProperty("Name").ValueKind.ShouldBe(JsonValueKind.String);
        group.GetProperty("DeepLevel").ValueKind.ShouldBe(JsonValueKind.Number);
        group.GetProperty("FirstCopy").ValueKind.ShouldBe(JsonValueKind.True);
        group.GetProperty("Description").ValueKind.ShouldBe(JsonValueKind.String);
        group.GetProperty("Path").ValueKind.ShouldBe(JsonValueKind.Array);
        group.GetProperty("PathIds").ValueKind.ShouldBe(JsonValueKind.Array);
        group.GetProperty("Characters").ValueKind.ShouldBe(JsonValueKind.Array);

        // Персонаж в ответе обязан быть: иначе проверки ниже (и рубеж на типизированные значения)
        // прошли бы по пустому массиву.
        var character = json.GetProperty("Groups").EnumerateArray()
            .SelectMany(g => g.GetProperty("Characters").EnumerateArray())
            .First();
        character.GetProperty("CharacterId").ValueKind.ShouldBe(JsonValueKind.Number);
        character.GetProperty("CharacterName").ValueKind.ShouldBe(JsonValueKind.String);
        character.GetProperty("CharacterLink").ValueKind.ShouldBe(JsonValueKind.String);
        character.GetProperty("ActiveClaimsCount").ValueKind.ShouldBe(JsonValueKind.Number);
    }

    /// <summary>
    /// Общий рубеж на весь ответ, включая персонажей: типизированное значение без <c>.Value</c>
    /// сериализуется как объект с единственным свойством <c>Value</c>. В этом ответе таких объектов
    /// быть не должно ни на каком уровне.
    /// </summary>
    [Fact]
    public async Task NoTypedValueLeaksAnywhereInResponse()
    {
        var json = await GetIndexJson();

        AssertNoTypedValueObjects(json, "$");
    }

    private static void AssertNoTypedValueObjects(JsonElement element, string path)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var properties = element.EnumerateObject().ToList();
                if (properties.Count == 1 && properties[0].NameEquals("Value"))
                {
                    throw new ShouldAssertException(
                        $"В публичном JSON ролей по пути {path} лежит объект {element.GetRawText()} — "
                        + "похоже, типизированное значение попало в ответ без .Value. "
                        + "Формат этого JSON менять нельзя: его читают внешние сайты игр.");
                }

                foreach (var property in properties)
                {
                    AssertNoTypedValueObjects(property.Value, $"{path}.{property.Name}");
                }

                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    AssertNoTypedValueObjects(item, $"{path}[{index++}]");
                }

                break;
        }
    }

    /// <summary>
    /// Ответ контроллера ровно в том виде, в котором его увидит внешний сайт: сериализуем теми же
    /// опциями, что вернул сам контроллер.
    /// </summary>
    private static async Task<JsonElement> GetIndexJson()
    {
        var mock = new MockedProject();

        // Публичный JSON показывает только публичные группы и роли — иначе в ответе будет пусто.
        foreach (var group in mock.Project.CharacterGroups)
        {
            group.IsPublic = true;
        }

        foreach (var character in mock.Project.Characters)
        {
            character.IsPublic = true;
        }

        mock.ReInitProjectInfo();

        var controller = new GameGroupsJsonController(
            new PublicCharacterJsonBuilder(new FakeUriLocator(), new FakeUriLocator(), new FakeUriLocator()),
            new FakeProjectMetadataRepository(mock),
            new FakeCharacterGroupRepository(mock),
            new FakeCharacterInfoRepository(mock),
            // Игрок, а не мастер: публичный JSON читают посторонние.
            new FakeCurrentUserAccessor(mock.Player.UserId))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        var result = (await controller.IndexJson(mock.ProjectInfo.GroupTree.RootGroupId)).ShouldBeOfType<JsonResult>();

        var json = JsonSerializer.Serialize(
            result.Value,
            (JsonSerializerOptions?)result.SerializerSettings ?? JsonSerializerOptions.Default);
        return JsonDocument.Parse(json).RootElement;
    }

    /// <summary>
    /// Описания групп грузятся отдельным запросом — отдаём их для всех групп проекта.
    /// </summary>
    private sealed class FakeCharacterGroupRepository(MockedProject mock) : ICharacterGroupRepository
    {
        public Task<CharacterGroupFullInfo?> GetCharacterGroupFullInfo(CharacterGroupIdentification id)
            => Task.FromResult<CharacterGroupFullInfo?>(Build(mock.ProjectInfo.GroupTree.AllGroups.Single(g => g.Id == id)));

        public Task<IReadOnlyList<CharacterGroupFullInfo>> GetCharacterGroupsFullInfo(
            IReadOnlyCollection<CharacterGroupIdentification> groupIds)
            => Task.FromResult<IReadOnlyList<CharacterGroupFullInfo>>(
                [.. mock.ProjectInfo.GroupTree.AllGroups.Where(g => groupIds.Contains(g.Id)).Select(Build)]);

        private static CharacterGroupFullInfo Build(CharacterGroupInfo group)
            => new(
                group,
                directChildCharactersCount: 0,
                description: new MarkdownString("Описание группы"),
                marks: new CreateUpdateMarksInfo(DateTime.UnixEpoch, null, DateTime.UnixEpoch, null));
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
