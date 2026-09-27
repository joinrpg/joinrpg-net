using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Subscribe;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Users;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Services.Interfaces.Subscribe;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Загрузка одной подписки по id — интеграционный тест, потому что ломается только на выполнении.
/// </summary>
/// <remarks>
/// Проектор подписки собран из двух выражений (<c>Invoke</c> из LinqKit), и без
/// <c>AsExpandable()</c> запрос компилируется, но падает в
/// <c>NotSupportedException</c> при обращении к базе. Юнит-тест такое не поймал бы: дерево
/// выражений само по себе корректно, разваливается именно трансляция в SQL.
///
/// На проде это было 500 на <c>/{projectId}/subscribe/EditRedirect</c> — единственной странице,
/// которая ходит через <see cref="IUserSubscribeRepository.LoadSubscriptionById"/>.
/// </remarks>
public class LoadSubscriptionByIdScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task SubscriptionLoadedById_HasGroupNameFromJoinedEntity()
    {
        UserIdentification masterId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId);
        }

        const string groupName = "Группа с подпиской";
        var groupId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<ICharacterGroupService>().AddCharacterGroup(
                projectId,
                groupName,
                isPublic: true,
                parentCharacterGroupIds: [projectInfo.GroupTree.RootGroupId],
                description: "");
        });

        var subscriptionId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            await sp.GetRequiredService<IGameSubscribeService>().UpdateSubscribeForGroup(new SubscribeForGroupRequest
            {
                CharacterGroupId = groupId,
                SubscriptionOptions = SubscriptionOptions.CreateAllSet(),
                MasterId = masterId.Value,
            });

            // Сервис подписки не возвращает id созданной записи, а страница адресует подписку именно им.
            return sp.GetRequiredService<MyDbContext>().Set<UserSubscription>()
                .Where(s => s.ProjectId == projectId.Value && s.UserId == masterId.Value)
                .Select(s => s.UserSubscriptionId)
                .First();
        });

        var subscription = await factory.Services.RunAsAsync(
            masterId,
            sp => sp.GetRequiredService<IUserSubscribeRepository>().LoadSubscriptionById(projectId, subscriptionId));

        subscription.ShouldNotBeNull();
        subscription.UserSubscriptionId.ShouldBe(subscriptionId);
        subscription.CharacterGroupId.ShouldBe(groupId.CharacterGroupId);

        // Имя группы приезжает из связанной сущности, и собирает его та самая часть проектора,
        // из-за которой запрос и разваливался.
        subscription.CharacterGroupName.ShouldBe(groupName);
        subscription.Options.AllSet.ShouldBeTrue();
    }
}
