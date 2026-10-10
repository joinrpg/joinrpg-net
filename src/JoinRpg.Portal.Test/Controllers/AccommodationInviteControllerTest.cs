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
/// Контроллер не накрыт <c>CaptureNoAccessExceptionFilter</c>, поэтому доменные отказы
/// он раскладывает сам: игрок должен увидеть русскую причину, а не 500-ю (#5260).
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
        nameof(AccommodationInviteController.AcceptInvite),
        nameof(AccommodationInviteController.DeclineInvite),
        nameof(AccommodationInviteController.CancelInvite),
        nameof(AccommodationInviteController.SetAccommodationType),
    ];

    private static Task<ActionResult> Invoke(AccommodationInviteController controller, string action) => action switch
    {
        nameof(AccommodationInviteController.CreateInvite) => controller.CreateInvite(ClaimId, Target),
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

    [Theory]
    [MemberData(nameof(Actions))]
    public async Task DeactivatedProjectIsBadRequestWithRussianReason(string action)
    {
        var result = await Invoke(Create(new ProjectDeactivatedException(ProjectId)), action);

        result.ShouldBeOfType<BadRequestObjectResult>().Value
            .ShouldBe("Проект находится в архиве, изменить проживание уже нельзя");
    }

    [Theory]
    [MemberData(nameof(Actions))]
    public async Task NoAccessIsBadRequestWithRussianReason(string action)
    {
        var mock = new MockedProject();

        var result = await Invoke(Create(new NoAccessToProjectException(mock.ProjectInfo, userId: 1)), action);

        result.ShouldBeOfType<BadRequestObjectResult>().Value
            .ShouldBe("Недостаточно прав для этого действия");
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
        public Task<IReadOnlyCollection<AccommodationInviteViewModel>> GetInvites(ClaimIdentification claimId, InviteDirection direction) => throw new NotSupportedException();
        public Task AcceptInvite(AccommodationInviteIdentification inviteId) => Fail();
        public Task DeclineInvite(AccommodationInviteIdentification inviteId) => Fail();
        public Task CancelInvite(AccommodationInviteIdentification inviteId) => Fail();
        public Task<AccommodationTypeChoiceViewModel> GetAccommodationTypes(ClaimIdentification claimId) => throw new NotSupportedException();
        public Task SetAccommodationType(ClaimIdentification claimId, AccommodationTypeIdentification typeId) => Fail();
    }
}
