using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Portal.Controllers.WebApi;
using JoinRpg.Web.Accommodation;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Test.Controllers;

/// <summary>
/// Отказ в приглашении контроллер отдаёт текстом, чтобы игрок увидел русскую причину, а не 500-ю (#5260).
/// Архив, блокировку и нехватку прав разбирает <c>WebApiExceptionHandler</c> (#5735).
/// </summary>
public class AccommodationInviteControllerTest
{
    private static readonly ProjectIdentification ProjectId = new(1611);
    private static readonly ClaimIdentification ClaimId = new(ProjectId, 3);
    private static readonly AccommodationInviteIdentification InviteId = new(ProjectId, 5);
    private static readonly AccommodationTypeIdentification TypeId = new(ProjectId, 7);
    private static readonly AccommodationGroupIdentification Target = AccommodationGroupIdentification.From(new ClaimIdentification(ProjectId, 9));

    public static TheoryData<string> Actions => [
        nameof(AccommodationInviteController.CreateInvite),
        nameof(AccommodationInviteController.MergeGroups),
        nameof(AccommodationInviteController.AcceptInvite),
        nameof(AccommodationInviteController.DeclineInvite),
        nameof(AccommodationInviteController.CancelInvite),
        nameof(AccommodationInviteController.SetAccommodationType),
    ];

    private static Task<ActionResult> Invoke(AccommodationInviteController controller, string action) => action switch
    {
        nameof(AccommodationInviteController.CreateInvite) => controller.CreateInvite(ClaimId, Target),
        nameof(AccommodationInviteController.MergeGroups) => controller.MergeGroups(ClaimId, Target),
        nameof(AccommodationInviteController.AcceptInvite) => controller.AcceptInvite(InviteId),
        nameof(AccommodationInviteController.DeclineInvite) => controller.DeclineInvite(InviteId),
        nameof(AccommodationInviteController.CancelInvite) => controller.CancelInvite(InviteId),
        nameof(AccommodationInviteController.SetAccommodationType) => controller.SetAccommodationType(ClaimId, TypeId),
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };

    private static AccommodationInviteController Create(Exception failure)
    {
        var client = new FakeClient(failure);
        return new AccommodationInviteController(client, client);
    }

    /// <summary>
    /// Блокировку проекта контроллер не выдаёт за архив: исключение уходит в <c>WebApiExceptionHandler</c>,
    /// который отвечает 409 с текстом про блокировку (#5735).
    /// </summary>
    [Theory]
    [MemberData(nameof(Actions))]
    public async Task BlockedProjectIsNotSwallowed(string action)
        => _ = await Should.ThrowAsync<ProjectBlockedException>(
            () => Invoke(Create(new ProjectBlockedException(ProjectId)), action));

    [Theory]
    [MemberData(nameof(Actions))]
    public async Task DeactivatedProjectIsLeftToExceptionHandler(string action)
        => _ = await Should.ThrowAsync<ProjectDeactivatedException>(
            () => Invoke(Create(new ProjectDeactivatedException(ProjectId)), action));

    [Theory]
    [MemberData(nameof(Actions))]
    public async Task NoAccessIsLeftToExceptionHandler(string action)
    {
        var mock = new MockedProject();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => Invoke(Create(new NoAccessToProjectException(mock.ProjectInfo, userId: 1)), action));
    }

    [Fact]
    public async Task CreateInviteNotAllowedPassesReasonFromService()
    {
        var controller = Create(new AccommodationInviteNotAllowedException(ProjectId, "Причина"));

        var result = await controller.CreateInvite(ClaimId, Target);

        result.ShouldBeOfType<BadRequestObjectResult>().Value.ShouldBe("Причина");
    }

    private sealed class FakeClient(Exception failure) : IAccommodationInviteClient, IAccommodationTypeClient
    {
        private Task Fail() => Task.FromException(failure);

        public Task<AccommodationInviteTargetsViewModel> GetInviteTargets(ClaimIdentification claimId) => throw new NotSupportedException();
        public Task CreateInvite(ClaimIdentification claimId, AccommodationGroupIdentification target) => Fail();
        public Task MergeGroups(ClaimIdentification claimId, AccommodationGroupIdentification target) => Fail();
        public Task<IReadOnlyCollection<AccommodationInviteViewModel>> GetInvites(ClaimIdentification claimId, InviteDirection direction) => throw new NotSupportedException();
        public Task AcceptInvite(AccommodationInviteIdentification inviteId) => Fail();
        public Task DeclineInvite(AccommodationInviteIdentification inviteId) => Fail();
        public Task CancelInvite(AccommodationInviteIdentification inviteId) => Fail();
        public Task<AccommodationTypeChoiceViewModel> GetAccommodationTypes(ClaimIdentification claimId) => throw new NotSupportedException();
        public Task SetAccommodationType(ClaimIdentification claimId, AccommodationTypeIdentification typeId) => Fail();
    }
}
