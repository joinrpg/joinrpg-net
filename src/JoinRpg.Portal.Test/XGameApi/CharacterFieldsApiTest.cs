using System.Text.Json;
using JoinRpg.Domain;
using JoinRpg.Portal.Controllers.XGameApi;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces.Characters;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Test.XGameApi;

public class CharacterFieldsApiTest
{
    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    private static CharacterApiController CreateController(ICharacterService characterService)
        => new(characterRepository: null!, characterService, projectMetadataRepository: null!);

    [Fact]
    public async Task SetCharacterFields_InvalidVariantId_ReturnsBadRequest()
    {
        var service = new ThrowingCharacterService(new FieldValueInvalidException("Поле с вариантами", 99999));
        var controller = CreateController(service);

        var result = await controller.SetCharacterFields(
            projectId: 1,
            characterId: 1,
            fieldValues: new Dictionary<int, JsonElement> { [42] = Parse("99999") });

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SetCharacterFields_Success_ReturnsOk()
    {
        var service = new ThrowingCharacterService(exceptionToThrow: null);
        var controller = CreateController(service);

        var result = await controller.SetCharacterFields(
            projectId: 1,
            characterId: 1,
            fieldValues: new Dictionary<int, JsonElement> { [42] = Parse("100") });

        result.Value.ShouldBe("ok");
    }

    private class ThrowingCharacterService(Exception? exceptionToThrow) : ICharacterService
    {
        public Task<CharacterIdentification> AddCharacter(AddCharacterRequest addCharacterRequest) => throw new NotImplementedException();
        public Task DeleteCharacter(DeleteCharacterRequest deleteCharacterRequest) => throw new NotImplementedException();
        public Task EditCharacter(EditCharacterRequest editCharacterRequest) => throw new NotImplementedException();
        public Task MoveCharacter(int currentUserId, int projectId, int characterId, int parentCharacterGroupId, short direction) => throw new NotImplementedException();

        public Task SetFields(CharacterIdentification characterId, Dictionary<int, string?> requestFieldValues)
        {
            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }
            return Task.CompletedTask;
        }
    }
}
