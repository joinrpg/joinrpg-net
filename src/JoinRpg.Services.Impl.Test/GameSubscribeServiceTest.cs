using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl.Test;

/// <summary>
/// Регрессия #6033: при замене RequestMasterAccess над сущностью на перегрузку над ProjectInfo
/// вызов потерялся, и подписаться на группу чужого проекта мог любой пользователь.
/// </summary>
public class GameSubscribeServiceTest
{
    private readonly MockedProject mock = new();

    private GameSubscribeService CreateService(int userId)
        => new(
            new FakeUnitOfWork(mock),
            new FakeCurrentUserAccessor(new UserIdentification(userId)),
            new FakeProjectMetadataRepository(mock));

    [Fact]
    public async Task UpdateSubscribeForGroup_AsPlayer_ThrowsNoAccess()
    {
        var service = CreateService(mock.Player.UserId);

        // MasterId совпадает с текущим пользователем: единственная причина отказа — отсутствие
        // доступа мастера. Проверка стоит до загрузки группы (FakeUnitOfWork на ней бросил бы
        // NotSupportedException), т.е. чужой проект даже не читается.
        await Should.ThrowAsync<NoAccessToProjectException>(() => service.UpdateSubscribeForGroup(
            new SubscribeForGroupRequest()
            {
                CharacterGroupId = new(mock.ProjectInfo.ProjectId, mock.Group.CharacterGroupId),
                SubscriptionOptions = SubscriptionOptions.CreateAllSet(),
                MasterId = mock.Player.UserId,
            }));
    }
}
