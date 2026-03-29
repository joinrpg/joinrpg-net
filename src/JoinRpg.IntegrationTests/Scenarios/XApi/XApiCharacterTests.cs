using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.XGameApi.Contract;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

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
        var characterId = await fixture.CreateNewCharacter(fixture.MasterUserId, projectId);
        var fieldId = await fixture.CreateField(fixture.MasterUserId, projectId, ProjectFieldType.String, "Строка");

        await fixture.MasterClient.SetCharacterFieldsAsync(projectId, characterId.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = "тестовое значение" });

        var result = await fixture.MasterClient.GetCharacterAsync(projectId, characterId.CharacterId);
        result.Fields.ShouldContain(f => f.ProjectFieldId == fieldId.ProjectFieldId && f.Value == "тестовое значение");
    }

    [Fact]
    public async Task CanSetTextField()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var characterId = await fixture.CreateNewCharacter(fixture.MasterUserId, projectId);
        var fieldId = await fixture.CreateField(fixture.MasterUserId, projectId, ProjectFieldType.Text, "Текст");

        await fixture.MasterClient.SetCharacterFieldsAsync(projectId, characterId.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = "многострочный\nтекст" });

        var result = await fixture.MasterClient.GetCharacterAsync(projectId, characterId.CharacterId);
        result.Fields.ShouldContain(f => f.ProjectFieldId == fieldId.ProjectFieldId && f.Value == "многострочный\nтекст");
    }

    [Fact]
    public async Task CanSetCheckboxField()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var characterId = await fixture.CreateNewCharacter(fixture.MasterUserId, projectId);
        var fieldId = await fixture.CreateField(fixture.MasterUserId, projectId, ProjectFieldType.Checkbox, "Чекбокс");

        await fixture.MasterClient.SetCharacterFieldsAsync(projectId, characterId.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = "on" });

        var result = await fixture.MasterClient.GetCharacterAsync(projectId, characterId.CharacterId);
        result.Fields.ShouldContain(f => f.ProjectFieldId == fieldId.ProjectFieldId && f.Value == "on");
    }

    [Fact]
    public async Task CanSetNumberField()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var characterId = await fixture.CreateNewCharacter(fixture.MasterUserId, projectId);
        var fieldId = await fixture.CreateField(fixture.MasterUserId, projectId, ProjectFieldType.Number, "Число");

        await fixture.MasterClient.SetCharacterFieldsAsync(projectId, characterId.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = 42 });

        var result = await fixture.MasterClient.GetCharacterAsync(projectId, characterId.CharacterId);
        result.Fields.ShouldContain(f => f.ProjectFieldId == fieldId.ProjectFieldId && f.Value == "42");
    }

    [Fact]
    public async Task CanSetDropdownField()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var characterId = await fixture.CreateNewCharacter(fixture.MasterUserId, projectId);
        var fieldId = await fixture.CreateField(fixture.MasterUserId, projectId, ProjectFieldType.Dropdown, "Список");
        var variantId = await fixture.CreateFieldVariant(fixture.MasterUserId, fieldId, "Вариант 1");

        await fixture.MasterClient.SetCharacterFieldsAsync(projectId, characterId.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = variantId });

        var result = await fixture.MasterClient.GetCharacterAsync(projectId, characterId.CharacterId);
        result.Fields.ShouldContain(f => f.ProjectFieldId == fieldId.ProjectFieldId && f.Value == variantId.ToString());
    }

    [Fact]
    public async Task CanSetMultiSelectField()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var characterId = await fixture.CreateNewCharacter(fixture.MasterUserId, projectId);
        var fieldId = await fixture.CreateField(fixture.MasterUserId, projectId, ProjectFieldType.MultiSelect, "Мультивыбор");
        var variantId1 = await fixture.CreateFieldVariant(fixture.MasterUserId, fieldId, "Вариант А");
        var variantId2 = await fixture.CreateFieldVariant(fixture.MasterUserId, fieldId, "Вариант Б");

        await fixture.MasterClient.SetCharacterFieldsAsync(projectId, characterId.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = new[] { variantId1, variantId2 } });

        var result = await fixture.MasterClient.GetCharacterAsync(projectId, characterId.CharacterId);
        var field = result.Fields.Single(f => f.ProjectFieldId == fieldId.ProjectFieldId);
        field.Value.ShouldNotBeNullOrEmpty();
        field.Value!.Split(',').ShouldContain(variantId1.ToString());
        field.Value.Split(',').ShouldContain(variantId2.ToString());
    }

    [Fact]
    public async Task CanSetLoginField()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var characterId = await fixture.CreateNewCharacter(fixture.MasterUserId, projectId);
        var fieldId = await fixture.CreateField(fixture.MasterUserId, projectId, ProjectFieldType.Login, "Логин");

        await fixture.MasterClient.SetCharacterFieldsAsync(projectId, characterId.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = "testuser" });

        var result = await fixture.MasterClient.GetCharacterAsync(projectId, characterId.CharacterId);
        result.Fields.ShouldContain(f => f.ProjectFieldId == fieldId.ProjectFieldId && f.Value == "testuser");
    }

    [Fact]
    public async Task CanSetUriField()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var characterId = await fixture.CreateNewCharacter(fixture.MasterUserId, projectId);
        var fieldId = await fixture.CreateField(fixture.MasterUserId, projectId, ProjectFieldType.Uri, "Ссылка");

        await fixture.MasterClient.SetCharacterFieldsAsync(projectId, characterId.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = "https://example.com" });

        var result = await fixture.MasterClient.GetCharacterAsync(projectId, characterId.CharacterId);
        result.Fields.ShouldContain(f => f.ProjectFieldId == fieldId.ProjectFieldId && f.Value == "https://example.com");
    }

    [Fact]
    public async Task CanSetPinCodeField()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var characterId = await fixture.CreateNewCharacter(fixture.MasterUserId, projectId);
        var fieldId = await fixture.CreateField(fixture.MasterUserId, projectId, ProjectFieldType.PinCode, "Пин-код");

        await fixture.MasterClient.SetCharacterFieldsAsync(projectId, characterId.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = "1234" });

        var result = await fixture.MasterClient.GetCharacterAsync(projectId, characterId.CharacterId);
        result.Fields.ShouldContain(f => f.ProjectFieldId == fieldId.ProjectFieldId && f.Value == "1234");
    }

    [Fact]
    public async Task CanClearField()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var characterId = await fixture.CreateNewCharacter(fixture.MasterUserId, projectId);
        var fieldId = await fixture.CreateField(fixture.MasterUserId, projectId, ProjectFieldType.String, "Очищаемое поле");

        await fixture.MasterClient.SetCharacterFieldsAsync(projectId, characterId.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = "значение" });

        await fixture.MasterClient.SetCharacterFieldsAsync(projectId, characterId.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = null });

        var result = await fixture.MasterClient.GetCharacterAsync(projectId, characterId.CharacterId);
        result.Fields.ShouldNotContain(f => f.ProjectFieldId == fieldId.ProjectFieldId && !string.IsNullOrEmpty(f.Value));
    }

    [Fact]
    public async Task SetHeaderField_ReturnsError()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var characterId = await fixture.CreateNewCharacter(fixture.MasterUserId, projectId);
        var fieldId = await fixture.CreateField(fixture.MasterUserId, projectId, ProjectFieldType.Header, "Заголовок");

        await Should.ThrowAsync<HttpRequestException>(
            () => fixture.MasterClient.SetCharacterFieldsAsync(projectId, characterId.CharacterId,
                new Dictionary<int, object?> { [fieldId.ProjectFieldId] = "какое-то значение" }));
    }
}
