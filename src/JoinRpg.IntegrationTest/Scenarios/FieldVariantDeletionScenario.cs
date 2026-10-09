using System.Data.Entity;
using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Удаление значений поля через ajax-ручки острова «Поля проекта»: одного значения, всех
/// неиспользованных (#5231) и поля целиком.
/// </summary>
/// <remarks>
/// У каждого значения выпадающего поля своя спецгруппа персонажей, и удаление решает, можно ли
/// стереть её насовсем, — по её вводным, дочерним группам и персонажам. Массовые ручки делают это в
/// цикле, поэтому на поле из одного значения N+1 не виден: на проде кнопка «Удалить все
/// неиспользованные» дала 18 догрузок <c>PlotElementCharacterGroups</c> за запрос (#5269). Поэтому
/// значений в сиде несколько. Ленивые загрузки проверяет <c>LazyLoadAssertingHandler</c> на клиенте
/// фабрики (см. docs/lazy-loads-baseline.md).
/// </remarks>
public class FieldVariantDeletionScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    private const int VariantCount = 4;

    [Fact]
    public async Task DeleteVariants_ThroughWebApi_RemovesThem()
    {
        var (masterId, email) = await CreateMasterAsync();
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект для удаления значений поля");
        }

        var (fieldId, variantIds) = await SeedDropdownFieldAsync(masterId, projectId, "Поле для чистки");
        var (fieldToDeleteId, _) = await SeedDropdownFieldAsync(masterId, projectId, "Поле на удаление");

        // Спецгруппа одного из значений стоит в таргетах вводной: такую группу стирать насовсем
        // нельзя, только выключить. Вводные спецгрупп грузятся заранее пачкой, и без этой проверки
        // недогруженная коллекция выглядела бы пустой — и группа со связями ушла бы из базы.
        var targetedGroupId = await SeedPlotTargetingVariantGroupAsync(masterId, projectId, fieldId, variantIds[1]);

        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), email);

        await PostAsync(client, projectId, "deletevariant", variantIds[0]);
        await PostAsync(client, projectId, "deleteunusedvariants", fieldId);
        await PostAsync(client, projectId, "delete", fieldToDeleteId);

        using var checkScope = factory.Services.CreateScope();
        var projectInfo = await checkScope.ServiceProvider.GetRequiredService<IProjectMetadataRepository>()
            .GetProjectMetadata(projectId);
        projectInfo.GetFieldById(fieldId).SortedVariants.Where(v => v.IsActive).ShouldBeEmpty();
        projectInfo.UnsortedFields.ShouldNotContain(f => f.Id == fieldToDeleteId && f.IsActive);

        var targetedGroup = await checkScope.ServiceProvider.GetRequiredService<MyDbContext>()
            .Set<CharacterGroup>()
            .Include(g => g.DirectlyRelatedPlotElements)
            .SingleAsync(g => g.CharacterGroupId == targetedGroupId.CharacterGroupId);
        targetedGroup.IsActive.ShouldBeFalse();
        targetedGroup.DirectlyRelatedPlotElements.Count.ShouldBe(1);
    }

    private async Task<CharacterGroupIdentification> SeedPlotTargetingVariantGroupAsync(
        UserIdentification masterId,
        ProjectIdentification projectId,
        ProjectFieldIdentification fieldId,
        ProjectFieldVariantIdentification variantId)
        => await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            var groupId = projectInfo.GetFieldById(fieldId).SortedVariants.Single(v => v.Id == variantId).CharacterGroupId
                ?? throw new InvalidOperationException("У значения выпадающего поля нет спецгруппы");

            var plotService = sp.GetRequiredService<IPlotService>();
            var folderId = await plotService.CreatePlotFolder(projectId, "Сюжет про значение поля", todo: "");
            _ = await plotService.CreatePlotElement(
                folderId,
                content: "Вводная для всех, кто выбрал значение",
                todoField: "",
                targetGroups: [groupId],
                targetChars: [],
                elementType: PlotElementType.RegularPlot,
                isMasterOnly: false);
            return groupId;
        });

    private static async Task PostAsync<T>(HttpClient client, ProjectIdentification projectId, string action, T body)
    {
        var response = await client.PostAsJsonAsync(
            $"webapi/project-field-operations/{action}?projectId={projectId.Value}",
            body);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, $"POST project-field-operations/{action}");
    }

    private async Task<(ProjectFieldIdentification FieldId, IReadOnlyList<ProjectFieldVariantIdentification> VariantIds)> SeedDropdownFieldAsync(
        UserIdentification masterId,
        ProjectIdentification projectId,
        string name)
        => await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var fieldSetupService = sp.GetRequiredService<IFieldSetupService>();
            var fieldId = await fieldSetupService.AddField(new CreateFieldRequest(
                projectId,
                ProjectFieldType.Dropdown,
                name,
                fieldHint: "",
                canPlayerEdit: true,
                canPlayerView: true,
                isPublic: true,
                FieldBoundTo.Character,
                MandatoryStatus.Optional,
                showForGroups: [],
                validForNpc: true,
                includeInPrint: true,
                showForUnapprovedClaims: true,
                price: 0,
                masterFieldHint: "",
                programmaticValue: null));

            var variantIds = new List<ProjectFieldVariantIdentification>(VariantCount);
            for (var i = 1; i <= VariantCount; i++)
            {
                variantIds.Add(await fieldSetupService.CreateFieldValueVariant(new CreateFieldValueVariantRequest(
                    fieldId,
                    $"Значение {i}",
                    description: null,
                    masterDescription: null,
                    programmaticValue: null,
                    price: 0,
                    playerSelectable: true,
                    timeSlotOptions: null)));
            }

            return (fieldId, (IReadOnlyList<ProjectFieldVariantIdentification>)variantIds);
        });

    private async Task<(UserIdentification MasterId, string Email)> CreateMasterAsync()
    {
        using var scope = factory.Services.CreateScope();
        return await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
    }
}
