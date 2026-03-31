using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.XGameApi.Contract;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

[Collection("XApi")]
public class XApiCharacterTests(XApiMasterFixture fixture)
{
    [Fact]
    public async Task WithoutAuth_Returns401()
    {
        await Should.ThrowAsync<HttpRequestException>(
            () => fixture.AnonymousXApiClient.GetCharactersAsync(fixture.ProjectId));
    }

    [Fact]
    public async Task MasterCanGetEmptyCharacterList()
    {
        var characters = await fixture.MasterClient.GetCharactersAsync(fixture.ProjectId);
        characters.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetNonexistentCharacter_ReturnsError()
    {
        await Should.ThrowAsync<HttpRequestException>(
            () => fixture.MasterClient.GetCharacterAsync(fixture.ProjectId, characterId: 999999));
    }

    [Fact]
    public async Task CanCreateCharacter()
    {
        var newProjectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var header = await fixture.MasterClient.CreateCharacterAsync(newProjectId, new CreateCharacterRequest());
        var result = await fixture.MasterClient.GetCharacterAsync(newProjectId, header.CharacterId);
        result.CharacterId.ShouldBe(header.CharacterId);
    }

    [Fact]
    public async Task CanSetStringField()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var character = await fixture.MasterClient.CreateCharacterAsync(projectId, new CreateCharacterRequest());
        var fieldId = await fixture.CreateField(fixture.MasterUserId, projectId, ProjectFieldType.String, "Строковое поле");

        await fixture.MasterClient.SetCharacterFieldsAsync(projectId, character.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = "hello" });

        var result = await fixture.MasterClient.GetCharacterAsync(projectId, character.CharacterId);
        result.Fields.ShouldContain(f => f.ProjectFieldId == fieldId.ProjectFieldId && f.Value == "hello");
    }

    [Fact]
    public async Task CanSetTextField()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var character = await fixture.MasterClient.CreateCharacterAsync(projectId, new CreateCharacterRequest());
        var fieldId = await fixture.CreateField(fixture.MasterUserId, projectId, ProjectFieldType.Text, "Текстовое поле");

        await fixture.MasterClient.SetCharacterFieldsAsync(projectId, character.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = "многострочный\nтекст" });

        var result = await fixture.MasterClient.GetCharacterAsync(projectId, character.CharacterId);
        result.Fields.ShouldContain(f => f.ProjectFieldId == fieldId.ProjectFieldId && f.Value == "многострочный\nтекст");
    }

    [Fact]
    public async Task CanSetCheckboxField()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var character = await fixture.MasterClient.CreateCharacterAsync(projectId, new CreateCharacterRequest());
        var fieldId = await fixture.CreateField(fixture.MasterUserId, projectId, ProjectFieldType.Checkbox, "Чекбокс");

        await fixture.MasterClient.SetCharacterFieldsAsync(projectId, character.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = "on" });

        var result = await fixture.MasterClient.GetCharacterAsync(projectId, character.CharacterId);
        result.Fields.ShouldContain(f => f.ProjectFieldId == fieldId.ProjectFieldId && f.Value == "on");
    }

    [Fact]
    public async Task CanSetNumberField()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var character = await fixture.MasterClient.CreateCharacterAsync(projectId, new CreateCharacterRequest());
        var fieldId = await fixture.CreateField(fixture.MasterUserId, projectId, ProjectFieldType.Number, "Числовое поле");

        await fixture.MasterClient.SetCharacterFieldsAsync(projectId, character.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = 42 });

        var result = await fixture.MasterClient.GetCharacterAsync(projectId, character.CharacterId);
        result.Fields.ShouldContain(f => f.ProjectFieldId == fieldId.ProjectFieldId && f.Value == "42");
    }

    [Fact]
    public async Task CanSetDropdownField()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var character = await fixture.MasterClient.CreateCharacterAsync(projectId, new CreateCharacterRequest());
        var fieldId = await fixture.CreateField(fixture.MasterUserId, projectId, ProjectFieldType.Dropdown, "Выпадающий список");
        var variantId = await fixture.CreateFieldVariant(fixture.MasterUserId, fieldId, "Вариант А");

        await fixture.MasterClient.SetCharacterFieldsAsync(projectId, character.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = variantId });

        var result = await fixture.MasterClient.GetCharacterAsync(projectId, character.CharacterId);
        result.Fields.ShouldContain(f => f.ProjectFieldId == fieldId.ProjectFieldId && f.Value == variantId.ToString());
    }

    [Fact]
    public async Task CanSetMultiSelectField()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var character = await fixture.MasterClient.CreateCharacterAsync(projectId, new CreateCharacterRequest());
        var fieldId = await fixture.CreateField(fixture.MasterUserId, projectId, ProjectFieldType.MultiSelect, "Мультивыбор");
        var variantId1 = await fixture.CreateFieldVariant(fixture.MasterUserId, fieldId, "Вариант 1");
        var variantId2 = await fixture.CreateFieldVariant(fixture.MasterUserId, fieldId, "Вариант 2");

        await fixture.MasterClient.SetCharacterFieldsAsync(projectId, character.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = new[] { variantId1, variantId2 } });

        var result = await fixture.MasterClient.GetCharacterAsync(projectId, character.CharacterId);
        result.Fields.ShouldContain(f => f.ProjectFieldId == fieldId.ProjectFieldId && f.Value == $"{variantId1},{variantId2}");
    }

    [Fact]
    public async Task CanSetLoginField()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var character = await fixture.MasterClient.CreateCharacterAsync(projectId, new CreateCharacterRequest());
        var fieldId = await fixture.CreateField(fixture.MasterUserId, projectId, ProjectFieldType.Login, "Логин");

        await fixture.MasterClient.SetCharacterFieldsAsync(projectId, character.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = "user123" });

        var result = await fixture.MasterClient.GetCharacterAsync(projectId, character.CharacterId);
        result.Fields.ShouldContain(f => f.ProjectFieldId == fieldId.ProjectFieldId && f.Value == "user123");
    }

    [Fact]
    public async Task CanSetUriField()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var character = await fixture.MasterClient.CreateCharacterAsync(projectId, new CreateCharacterRequest());
        var fieldId = await fixture.CreateField(fixture.MasterUserId, projectId, ProjectFieldType.Uri, "Ссылка");

        await fixture.MasterClient.SetCharacterFieldsAsync(projectId, character.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = "https://example.com" });

        var result = await fixture.MasterClient.GetCharacterAsync(projectId, character.CharacterId);
        result.Fields.ShouldContain(f => f.ProjectFieldId == fieldId.ProjectFieldId && f.Value == "https://example.com");
    }

    [Fact]
    public async Task CanSetPinCodeField()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var character = await fixture.MasterClient.CreateCharacterAsync(projectId, new CreateCharacterRequest());
        var fieldId = await fixture.CreateField(fixture.MasterUserId, projectId, ProjectFieldType.PinCode, "Пин-код");

        await fixture.MasterClient.SetCharacterFieldsAsync(projectId, character.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = "1234" });

        var result = await fixture.MasterClient.GetCharacterAsync(projectId, character.CharacterId);
        result.Fields.ShouldContain(f => f.ProjectFieldId == fieldId.ProjectFieldId && f.Value == "1234");
    }

    [Fact]
    public async Task SetHeaderField_Returns400()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var character = await fixture.MasterClient.CreateCharacterAsync(projectId, new CreateCharacterRequest());
        var fieldId = await fixture.CreateField(fixture.MasterUserId, projectId, ProjectFieldType.Header, "Заголовок");

        var statusCode = await fixture.MasterClient.SetCharacterFieldsRawAsync(projectId, character.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = "some value" });

        statusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetDropdownField_InvalidVariantId_Returns400()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var character = await fixture.MasterClient.CreateCharacterAsync(projectId, new CreateCharacterRequest());
        var fieldId = await fixture.CreateField(fixture.MasterUserId, projectId, ProjectFieldType.Dropdown, "Выпадающий список");
        _ = await fixture.CreateFieldVariant(fixture.MasterUserId, fieldId, "Вариант А");

        var statusCode = await fixture.MasterClient.SetCharacterFieldsRawAsync(projectId, character.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = 99999 });

        statusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CanClearField()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var character = await fixture.MasterClient.CreateCharacterAsync(projectId, new CreateCharacterRequest());
        var fieldId = await fixture.CreateField(fixture.MasterUserId, projectId, ProjectFieldType.String, "Поле для очистки");

        await fixture.MasterClient.SetCharacterFieldsAsync(projectId, character.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = "временное значение" });

        await fixture.MasterClient.SetCharacterFieldsAsync(projectId, character.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = null });

        var result = await fixture.MasterClient.GetCharacterAsync(projectId, character.CharacterId);
        result.Fields.ShouldNotContain(f => f.ProjectFieldId == fieldId.ProjectFieldId);
    }
}
