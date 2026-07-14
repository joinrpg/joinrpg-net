using JoinRpg.Domain;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Impl.Projects.Metadata;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl.Test.Projects;

public class FieldSetupServiceTest : ProjectMetadataServiceTestBase
{
    private FieldSetupServiceImpl CreateService(int? currentUserId = null, bool isAdmin = false)
    {
        var currentUser = CreateCurrentUser(currentUserId, isAdmin);
        return new FieldSetupServiceImpl(CreatePropsService(currentUser));
    }

    private CreateFieldRequest CreateFieldRequest(
        ProjectFieldType fieldType = ProjectFieldType.String,
        string name = "Новое поле")
        => new(
            ProjectId,
            fieldType,
            name,
            fieldHint: "",
            canPlayerEdit: true,
            canPlayerView: true,
            isPublic: false,
            FieldBoundTo.Character,
            MandatoryStatus.Optional,
            showForGroups: [],
            validForNpc: true,
            includeInPrint: true,
            showForUnapprovedClaims: true,
            price: 0,
            masterFieldHint: "",
            programmaticValue: null);

    [Fact]
    public async Task AddField_ByMaster_AddsFieldAndKeepsProjectInfoConsistent()
    {
        var service = CreateService(mock.Master.UserId);

        var result = await service.AddField(CreateFieldRequest(name: "Поле мастера"));

        result.ProjectId.ShouldBe(ProjectId);
        mock.Project.ProjectFields.ShouldContain(f => f.FieldName == "Поле мастера");
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
        // Пересобранный ProjectInfo положен в кэш и содержит новое поле.
        Result.UnsortedFields.ShouldContain(f => f.Name == "Поле мастера");
    }

    [Fact]
    public async Task AddField_ByPlayerWithoutAccess_Throws_AndDoesNotSave()
    {
        var service = CreateService(mock.Player.UserId);

        await Should.ThrowAsync<NoAccessToProjectException>(
            () => service.AddField(CreateFieldRequest()));

        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task AddField_OnInactiveProject_Throws()
    {
        mock.Project.Active = false;
        mock.Project.IsAcceptingClaims = false;
        var service = CreateService(mock.Master.UserId);

        await Should.ThrowAsync<ProjectDeactivatedException>(
            () => service.AddField(CreateFieldRequest()));

        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task AddField_DuplicateScheduleTimeSlot_Throws()
    {
        _ = mock.AddField(f => f.FieldType = ProjectFieldType.ScheduleTimeSlotField);
        var service = CreateService(mock.Master.UserId);

        await Should.ThrowAsync<JoinFieldScheduleShouldBeUniqueException>(
            () => service.AddField(CreateFieldRequest(ProjectFieldType.ScheduleTimeSlotField)));

        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }
}
