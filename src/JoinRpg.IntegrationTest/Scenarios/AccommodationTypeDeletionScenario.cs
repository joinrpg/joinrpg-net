using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.ProjectMetadata;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Удаление типа проживания на настоящей БД (ADR020, §4): последний тип категории уносит её с
/// собой, а комнаты категории удаляет каскад.
/// </summary>
/// <remarks>
/// Юнит-тесты сервиса идут на фейке, который сам разбирает навигации, — порядок удаления «тип,
/// потом категория» в одном <c>SaveChanges</c> и каскад «категория → комнаты» они не видят.
/// </remarks>
public class AccommodationTypeDeletionScenario(JoinApplicationFactory factory)
    : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task DeleteLastTypeOfCategory_RemovesCategoryAndItsRooms()
    {
        var ownerId = await CreateUserAsync();
        var projectId = await CreateProjectWithAccommodationAsync(ownerId);

        var typeId = await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IAccommodationTypeService>().CreateAccommodationType(
                projectId,
                new AccommodationTypeRequest("Палатка", new MarkdownString(""), Cost: 0, Capacity: 2, IsPlayerSelectable: true)));

        _ = await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<IAccommodationService>().AddRooms(
                projectInfo.AccommodationSettings.GetTypeById(typeId).RoomCategoryId,
                ["1", "2"]);
        });

        await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IAccommodationTypeService>().DeleteAccommodationType(typeId));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        db.Set<ProjectRoomCategory>().Count(c => c.ProjectId == projectId.Value).ShouldBe(0);
        db.Set<ProjectAccommodation>().Count(r => r.ProjectId == projectId.Value).ShouldBe(0);

        var metadata = await scope.ServiceProvider.GetRequiredService<IProjectMetadataRepository>()
            .GetProjectMetadata(projectId);
        metadata.AccommodationSettings.Types.ShouldBeEmpty();
        metadata.AccommodationSettings.RoomCategories.ShouldBeEmpty();
    }

    private async Task<UserIdentification> CreateUserAsync()
    {
        using var scope = factory.Services.CreateScope();
        return (await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider)).userId;
    }

    private async Task<ProjectIdentification> CreateProjectWithAccommodationAsync(UserIdentification ownerId)
    {
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, ownerId, "Проект для удаления типа проживания");
        }

        await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IProjectService>().SetAccommodationSettings(projectId, enableAccommodation: true));

        return projectId;
    }
}
