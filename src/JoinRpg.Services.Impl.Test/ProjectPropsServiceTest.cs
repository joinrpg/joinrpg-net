using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Impl.Projects;
using JoinRpg.Services.Impl.Test.Projects;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Services.Impl.Test;

public class ProjectPropsServiceTest
{
    private readonly MockedProject mock = new();
    private readonly FakeUnitOfWork unitOfWork;
    private readonly FakeProjectMetadataRepository metadataRepository;

    public ProjectPropsServiceTest()
    {
        unitOfWork = new FakeUnitOfWork(mock);
        metadataRepository = new FakeProjectMetadataRepository(mock);
    }

    private ProjectIdentification ProjectId => mock.ProjectInfo.ProjectId;

    private ProjectPropsService CreateService(int currentUserId, bool isAdmin = false)
        => new(
            unitOfWork,
            new FakeCurrentUserAccessor(currentUserId, isAdmin),
            metadataRepository,
            NullLogger<ProjectPropsService>.Instance);

    [Fact]
    public async Task Master_ChangesSetting_AppliesMutationAndKeepsProjectInfoConsistent()
    {
        var service = CreateService(mock.Master.UserId);

        await service.ChangeProjectProperties(
            ProjectId,
            Permission.CanChangeProjectProperties,
            ProjectActiveRequirement.AllowInactive,
            true,
            (project, _, enabled) => project.Details.EnableAccommodation = enabled);

        // Мутация применена к EF-сущности
        mock.Project.Details.EnableAccommodation.ShouldBeTrue();
        // Изменения сохранены
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
        // Пересобранный ProjectInfo согласован с Project и положен в кэш
        metadataRepository.LastPrimed.ShouldNotBeNull();
        metadataRepository.LastPrimed!.AccomodationEnabled.ShouldBeTrue();
    }

    [Fact]
    public async Task Func_Overload_ReturnsResult()
    {
        var service = CreateService(mock.Master.UserId);

        var result = await service.ChangeProjectProperties(
            ProjectId,
            Permission.CanChangeProjectProperties,
            ProjectActiveRequirement.AllowInactive,
            true,
            (project, _, enabled) =>
            {
                project.Details.EnableAccommodation = enabled;
                return 42;
            });

        result.ShouldBe(42);
    }

    [Fact]
    public async Task UserWithoutAccess_Throws_AndDoesNotSave()
    {
        // Player (id 1) не входит в ACL проекта
        var service = CreateService(mock.Player.UserId);

        await Should.ThrowAsync<NoAccessToProjectException>(() => service.ChangeProjectProperties(
            ProjectId,
            Permission.CanChangeProjectProperties,
            ProjectActiveRequirement.AllowInactive,
            true,
            (project, _, enabled) => project.Details.EnableAccommodation = enabled));

        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Admin_BypassesMasterAccessCheck()
    {
        // Player (id 1) без мастерского доступа, но админ
        var service = CreateService(mock.Player.UserId, isAdmin: true);

        await service.ChangeProjectProperties(
            ProjectId,
            Permission.CanChangeProjectProperties,
            ProjectActiveRequirement.AllowInactive,
            true,
            (project, _, enabled) => project.Details.EnableAccommodation = enabled);

        mock.Project.Details.EnableAccommodation.ShouldBeTrue();
    }

    [Fact]
    public async Task InactiveProject_MustBeActive_Throws()
    {
        mock.Project.Active = false;
        mock.Project.IsAcceptingClaims = false;
        var service = CreateService(mock.Master.UserId);

        await Should.ThrowAsync<ProjectDeactivatedException>(() => service.ChangeProjectProperties(
            ProjectId,
            Permission.CanChangeProjectProperties,
            ProjectActiveRequirement.MustBeActive,
            true,
            (project, _, enabled) => project.Details.EnableAccommodation = enabled));

        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task InactiveProject_AllowInactive_Succeeds()
    {
        mock.Project.Active = false;
        mock.Project.IsAcceptingClaims = false;
        var service = CreateService(mock.Master.UserId);

        await service.ChangeProjectProperties(
            ProjectId,
            Permission.CanChangeProjectProperties,
            ProjectActiveRequirement.AllowInactive,
            true,
            (project, _, enabled) => project.Details.EnableAccommodation = enabled);

        mock.Project.Details.EnableAccommodation.ShouldBeTrue();
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }
}
