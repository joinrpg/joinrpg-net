using System.Net;
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
        var newProjectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var header = await fixture.MasterClient.CreateCharacterAsync(newProjectId, new CreateCharacterRequest());
        var fieldId = await fixture.CreateField(fixture.MasterUserId, newProjectId, ProjectFieldType.String, "Строка");

        await fixture.MasterClient.SetCharacterFieldsAsync(newProjectId, header.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = "hello" });

        var result = await fixture.MasterClient.GetCharacterAsync(newProjectId, header.CharacterId);
        var field = result.Fields.ShouldHaveSingleItem();
        field.ProjectFieldId.ShouldBe(fieldId.ProjectFieldId);
        field.Value.ShouldBe("hello");
    }

    [Fact]
    public async Task CanSetTextField()
    {
        var newProjectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var header = await fixture.MasterClient.CreateCharacterAsync(newProjectId, new CreateCharacterRequest());
        var fieldId = await fixture.CreateField(fixture.MasterUserId, newProjectId, ProjectFieldType.Text, "Текст");

        await fixture.MasterClient.SetCharacterFieldsAsync(newProjectId, header.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = "многострочный\nтекст" });

        var result = await fixture.MasterClient.GetCharacterAsync(newProjectId, header.CharacterId);
        var field = result.Fields.ShouldHaveSingleItem();
        field.ProjectFieldId.ShouldBe(fieldId.ProjectFieldId);
        field.Value.ShouldBe("многострочный\nтекст");
    }

    [Fact]
    public async Task CanSetCheckboxField()
    {
        var newProjectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var header = await fixture.MasterClient.CreateCharacterAsync(newProjectId, new CreateCharacterRequest());
        var fieldId = await fixture.CreateField(fixture.MasterUserId, newProjectId, ProjectFieldType.Checkbox, "Флажок");

        await fixture.MasterClient.SetCharacterFieldsAsync(newProjectId, header.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = true });

        var result = await fixture.MasterClient.GetCharacterAsync(newProjectId, header.CharacterId);
        var field = result.Fields.ShouldHaveSingleItem();
        field.ProjectFieldId.ShouldBe(fieldId.ProjectFieldId);
        field.Value.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task CanSetNumberField()
    {
        var newProjectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var header = await fixture.MasterClient.CreateCharacterAsync(newProjectId, new CreateCharacterRequest());
        var fieldId = await fixture.CreateField(fixture.MasterUserId, newProjectId, ProjectFieldType.Number, "Число");

        await fixture.MasterClient.SetCharacterFieldsAsync(newProjectId, header.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = 42 });

        var result = await fixture.MasterClient.GetCharacterAsync(newProjectId, header.CharacterId);
        var field = result.Fields.ShouldHaveSingleItem();
        field.ProjectFieldId.ShouldBe(fieldId.ProjectFieldId);
        field.Value.ShouldBe("42");
    }

    [Fact]
    public async Task CanSetDropdownField()
    {
        var newProjectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var header = await fixture.MasterClient.CreateCharacterAsync(newProjectId, new CreateCharacterRequest());
        var fieldId = await fixture.CreateField(fixture.MasterUserId, newProjectId, ProjectFieldType.Dropdown, "Список");
        var variantId = await fixture.CreateFieldVariant(fixture.MasterUserId, fieldId, "Вариант А");

        await fixture.MasterClient.SetCharacterFieldsAsync(newProjectId, header.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = variantId });

        var result = await fixture.MasterClient.GetCharacterAsync(newProjectId, header.CharacterId);
        var field = result.Fields.ShouldHaveSingleItem();
        field.ProjectFieldId.ShouldBe(fieldId.ProjectFieldId);
        field.Value.ShouldBe(variantId.ToString());
    }

    [Fact]
    public async Task CanSetMultiSelectField()
    {
        var newProjectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var header = await fixture.MasterClient.CreateCharacterAsync(newProjectId, new CreateCharacterRequest());
        var fieldId = await fixture.CreateField(fixture.MasterUserId, newProjectId, ProjectFieldType.MultiSelect, "Мультивыбор");
        var variantId1 = await fixture.CreateFieldVariant(fixture.MasterUserId, fieldId, "Вариант 1");
        var variantId2 = await fixture.CreateFieldVariant(fixture.MasterUserId, fieldId, "Вариант 2");

        await fixture.MasterClient.SetCharacterFieldsAsync(newProjectId, header.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = new[] { variantId1, variantId2 } });

        var result = await fixture.MasterClient.GetCharacterAsync(newProjectId, header.CharacterId);
        var field = result.Fields.ShouldHaveSingleItem();
        field.ProjectFieldId.ShouldBe(fieldId.ProjectFieldId);
        var value = field.Value.ShouldNotBeNull();
        value.ShouldContain(variantId1.ToString());
        value.ShouldContain(variantId2.ToString());
    }

    [Fact]
    public async Task CanSetLoginField()
    {
        var newProjectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var header = await fixture.MasterClient.CreateCharacterAsync(newProjectId, new CreateCharacterRequest());
        var fieldId = await fixture.CreateField(fixture.MasterUserId, newProjectId, ProjectFieldType.Login, "Логин");

        await fixture.MasterClient.SetCharacterFieldsAsync(newProjectId, header.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = "player_login" });

        var result = await fixture.MasterClient.GetCharacterAsync(newProjectId, header.CharacterId);
        var field = result.Fields.ShouldHaveSingleItem();
        field.ProjectFieldId.ShouldBe(fieldId.ProjectFieldId);
        field.Value.ShouldBe("player_login");
    }

    [Fact]
    public async Task CanSetUriField()
    {
        var newProjectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var header = await fixture.MasterClient.CreateCharacterAsync(newProjectId, new CreateCharacterRequest());
        var fieldId = await fixture.CreateField(fixture.MasterUserId, newProjectId, ProjectFieldType.Uri, "Ссылка");

        await fixture.MasterClient.SetCharacterFieldsAsync(newProjectId, header.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = "https://example.com" });

        var result = await fixture.MasterClient.GetCharacterAsync(newProjectId, header.CharacterId);
        var field = result.Fields.ShouldHaveSingleItem();
        field.ProjectFieldId.ShouldBe(fieldId.ProjectFieldId);
        field.Value.ShouldBe("https://example.com");
    }

    [Fact]
    public async Task CanSetPinCodeField()
    {
        var newProjectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var header = await fixture.MasterClient.CreateCharacterAsync(newProjectId, new CreateCharacterRequest());
        var fieldId = await fixture.CreateField(fixture.MasterUserId, newProjectId, ProjectFieldType.PinCode, "Пин-код");

        await fixture.MasterClient.SetCharacterFieldsAsync(newProjectId, header.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = "1234" });

        var result = await fixture.MasterClient.GetCharacterAsync(newProjectId, header.CharacterId);
        var field = result.Fields.ShouldHaveSingleItem();
        field.ProjectFieldId.ShouldBe(fieldId.ProjectFieldId);
        field.Value.ShouldBe("1234");
    }

    [Fact]
    public async Task CanClearField()
    {
        var newProjectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var header = await fixture.MasterClient.CreateCharacterAsync(newProjectId, new CreateCharacterRequest());
        var fieldId = await fixture.CreateField(fixture.MasterUserId, newProjectId, ProjectFieldType.String, "Поле для очистки");

        await fixture.MasterClient.SetCharacterFieldsAsync(newProjectId, header.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = "значение" });

        await fixture.MasterClient.SetCharacterFieldsAsync(newProjectId, header.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = null });

        var result = await fixture.MasterClient.GetCharacterAsync(newProjectId, header.CharacterId);
        result.Fields.ShouldBeEmpty();
    }

    [Fact]
    public async Task SetHeaderField_ReturnsError()
    {
        var newProjectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var header = await fixture.MasterClient.CreateCharacterAsync(newProjectId, new CreateCharacterRequest());
        var fieldId = await fixture.CreateField(fixture.MasterUserId, newProjectId, ProjectFieldType.Header, "Заголовок");

        var response = await fixture.MasterClient.SetCharacterFieldsRawAsync(newProjectId, header.CharacterId,
            new Dictionary<int, object?> { [fieldId.ProjectFieldId] = "нельзя" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
