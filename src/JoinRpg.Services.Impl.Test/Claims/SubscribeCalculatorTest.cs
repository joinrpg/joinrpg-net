using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Impl.Claims;

namespace JoinRpg.Services.Impl.Test.Claims;

/// <summary>
/// Ответственный мастер в расчёте получателей уведомления по заявке.
/// </summary>
public class SubscribeCalculatorTest
{
    private readonly MockedProject mock = new();

    private Task<IReadOnlyCollection<NotificationRecepient>> GetRecepients(UserIdentification responsibleMaster)
    {
        var claimsRepository = new FakeClaimsRepository(mock);
        var calculator = new SubscribeCalculator(new FakeUserSubscribeRepository(), new FakeCharacterInfoRepository(mock), claimsRepository);
        return calculator.GetRecepients(
            new SubscribeCalculateArgs(
                Predicate: _ => true,
                Initiator: null,
                Player: [],
                RespMasters: [responsibleMaster],
                Claims: [],
                Characters: [],
                Finance: [],
                RespondingTo: []),
            mock.ProjectInfo);
    }

    [Fact]
    public async Task ActiveResponsibleMaster_IsRecepient()
    {
        var master = new UserIdentification(mock.Master.UserId);

        (await GetRecepients(master)).ShouldContain(r => r.UserId == master && r.SubscriptionReason == SubscriptionReason.ResponsibleMaster);
    }

    [Fact]
    public async Task RemovedResponsibleMaster_IsSkipped()
    {
        // У старой заявки ответственным мог остаться уже снятый мастер (ADR019) — доступа у него нет.
        var removed = mock.CreateMaster();
        mock.Project.ProjectAcls.Single(acl => acl.UserId == removed.UserId).Status = ProjectAclStatus.Removed;
        mock.ReInitProjectInfo();

        (await GetRecepients(new UserIdentification(removed.UserId))).ShouldBeEmpty();
    }
}
