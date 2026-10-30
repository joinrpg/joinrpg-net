using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Web.Schedule;
using JoinRpg.WebPortal.Managers.Interfaces;
using JoinRpg.WebPortal.Managers.Schedule;

namespace JoinRpg.WebPortal.Managers.Test.Schedule;

/// <summary>
/// Кто видит неопубликованное расписание: мастер всегда, игрок — только с принятой заявкой и
/// если поля расписания видны игрокам. Заявку игрока менеджер берёт из <c>UserInfo</c>,
/// а не из EF-графа проекта.
/// </summary>
public class ScheduleAccessTest
{
    private MockedProject Mock { get; } = new MockedProject();

    private void CreateScheduleFields(ProjectFieldVisibility visibility)
    {
        _ = Mock.CreateField("Помещение", visibility, fieldType: ProjectFieldType.ScheduleRoomField);
        _ = Mock.CreateField("Время", visibility, fieldType: ProjectFieldType.ScheduleTimeSlotField);
    }

    private Task<IReadOnlyCollection<ScheduleConfigProblemsViewModel>> Check(int userId, FakeUserRepository? userRepository = null)
        => new SchedulePageManager(
            new FakeCurrentProjectAccessor(Mock.ProjectInfo.ProjectId),
            new FakeCurrentUserAccessor(userId),
            new FakeProjectMetadataRepository(Mock),
            new FakeCharacterInfoRepository(Mock),
            userRepository ?? new FakeUserRepository(Mock))
            .CheckScheduleConfiguration();

    [Fact]
    public async Task PlayerWithApprovedClaim_HasAccess()
    {
        CreateScheduleFields(ProjectFieldVisibility.PlayerAndMaster);
        _ = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);

        (await Check(Mock.Player.UserId)).ShouldNotContain(ScheduleConfigProblemsViewModel.NoAccess);
    }

    [Fact]
    public async Task PlayerWithNotApprovedClaim_HasNoAccess()
    {
        CreateScheduleFields(ProjectFieldVisibility.PlayerAndMaster);
        _ = Mock.CreateClaim(Mock.Character, Mock.Player);

        (await Check(Mock.Player.UserId)).ShouldContain(ScheduleConfigProblemsViewModel.NoAccess);
    }

    [Fact]
    public async Task PlayerWithApprovedClaimInOtherProject_HasNoAccess()
    {
        CreateScheduleFields(ProjectFieldVisibility.PlayerAndMaster);
        var otherProject = new ProjectIdentification(Mock.ProjectInfo.ProjectId.Value + 1);
        var player = Mock.PlayerInfo with
        {
            ActiveClaims = [new UserClaimInfo(new ClaimIdentification(otherProject, 1), ClaimStatus.Approved)],
        };

        (await Check(Mock.Player.UserId, FakeUserRepository.WithUsers(player)))
            .ShouldContain(ScheduleConfigProblemsViewModel.NoAccess);
    }

    [Fact]
    public async Task PlayerWithApprovedClaim_HasNoAccess_WhenFieldsAreMasterOnly()
    {
        CreateScheduleFields(ProjectFieldVisibility.MasterOnly);
        _ = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);

        (await Check(Mock.Player.UserId)).ShouldContain(ScheduleConfigProblemsViewModel.NoAccess);
    }

    [Fact]
    public async Task Master_HasAccess_WithoutLoadingUser()
    {
        CreateScheduleFields(ProjectFieldVisibility.MasterOnly);

        var problems = await Check(
            Mock.Master.UserId,
            FakeUserRepository.MustNotBeCalled("мастеру заявки для доступа к расписанию не нужны"));

        problems.ShouldNotContain(ScheduleConfigProblemsViewModel.NoAccess);
    }

    private sealed class FakeCurrentProjectAccessor(ProjectIdentification projectId) : ICurrentProjectAccessor
    {
        public ProjectIdentification ProjectId => projectId;
    }
}
