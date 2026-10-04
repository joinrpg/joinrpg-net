using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Запрос <c>CharacterInfoLoader</c> на живой БД (ADR013): проверяем, что он вообще
/// транслируется в SQL и что скалярные флаги заявки приезжают верными.
/// </summary>
/// <remarks>
/// <para>
/// Юнит-тестами это не покрывается принципиально. <c>CharacterInfoMapperTest</c> проверяет только
/// маппинг готовых row-объектов, а <c>FinancePredicatesTest</c> гоняет <c>Compile()</c>, то есть
/// LINQ to Objects. Ни то, ни другое не скажет, умеет ли EF6 перевести проекцию в SQL.
/// </para>
/// <para>
/// Особенно это важно для <c>ClaimFinanceInfo.OperationsRequireModeration</c>:
/// правило живёт в <c>FinancePredicates.RequireModeration()</c> и подставляется в запрос
/// LinqKit'ом через <c>.Invoke()</c> внутри вложенного <c>Select</c>. Если такая подстановка
/// перестанет работать, запрос упадёт в рантайме с <c>NotSupportedException</c> — и уронить его
/// должен этот тест, а не прод.
/// </para>
/// </remarks>
public class CharacterInfoLoaderScenario(JoinApplicationFactory factory)
    : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task GetCharacterInfo_LoadsClaimScalarFlagsFromDatabase()
    {
        // 1. Мастер, проект, игрок. Игрок — ВТОРОЙ пользователь: заявка мастера на своего же
        // персонажа пошла бы другим путём (AddClaimFromMaster) и не дала бы согласия игрока.
        UserIdentification masterId;
        ProjectIdentification projectId;
        UserIdentification playerId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId);
            playerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        }

        // 2. Мастер открывает приём заявок и создаёт персонажа
        var characterId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectService = sp.GetRequiredService<IProjectService>();
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await projectService.SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });

            // Паспорт обязателен — иначе проект чувствительных данных не спрашивает, и согласие
            // игрока вообще не записывается: ClaimServiceImpl.AddClaimFromUser проставляет
            // PlayerAllowedSenstiveData только при ProfileRequirementSettings.SensitiveDataRequired.
            // Без этой строки флаг остался бы false при sensitiveDataAllowed: true.
            await projectService.SetContactSettings(
                projectId,
                ProjectProfileRequirementSettings.AllNotRequired with { RequirePassport = MandatoryStatus.Required });

            var characterService = sp.GetRequiredService<ICharacterService>();
            return await characterService.AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: FieldLayerContainer.Empty(projectInfo)));
        });

        // 3. Игрок подаёт заявку, разрешая доступ к чувствительным данным
        var claimId = await factory.Services.RunAsAsync(playerId, async sp =>
        {
            var claimService = sp.GetRequiredService<IClaimService>();
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await claimService.AddClaimFromUser(
                characterId, "Хочу эту роль", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: true);
        });

        // 4. Агрегат грузится через ICharacterInfoRepository — это и есть путь чтения,
        // целиком лежащий на CharacterInfoLoader. Сам факт, что мы дошли до assert'ов,
        // уже означает, что проекция перевелась в SQL.
        using (var scope = factory.Services.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ICharacterInfoRepository>();
            var claim = (await repository.GetCharacterInfo(characterId)).Claims.ShouldHaveSingleItem();

            claim.ClaimId.ShouldBe(claimId);
            // Согласие игрока доехало из колонки PlayerAllowedSenstiveData (опечатка — в БД).
            claim.PlayerAllowedSensitiveData.ShouldBeTrue();
            // Проживание в этой заявке не выбрано: опциональная навигация обязана дать null,
            // а не 0. Иначе печать конверта искала бы в метаданных тип с id 0 — упала бы или
            // показала мусор вместо «Поселение: нет», причём далеко от DAL.
            claim.AccommodationTypeId.ShouldBeNull();
            // Финансовых операций по заявке пока нет — модерировать нечего.
            claim.Finance.OperationsRequireModeration.ShouldBeFalse();
        }

        // 5. Игрок просит льготный взнос. Это штатный сервисный путь, создающий операцию с
        // State = Proposed и OperationType = PreferentialFeeRequest, то есть ровно ту,
        // для которой FinanceOperation.RequireModeration истинно.
        await factory.Services.RunAsAsync(playerId, async sp =>
            await sp.GetRequiredService<IFinanceService>().RequestPreferentialFee(
                new MarkMeAsPreferentialFeeOperationRequest
                {
                    ProjectId = projectId.Value,
                    ClaimId = claimId.ClaimId,
                    Contents = "Прошу льготный взнос",
                    OperationDate = DateTime.UtcNow,
                }));

        // 6. Тот же запрос на новом scope (без кеша контекста) должен увидеть операцию,
        // ждущую модерации. Проверка «не всегда false»: если предикат в SQL разойдётся с
        // доменным правилом, флаг останется ложным и тест упадёт.
        using (var scope = factory.Services.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ICharacterInfoRepository>();
            var claim = (await repository.GetCharacterInfo(characterId)).Claims.ShouldHaveSingleItem();

            claim.Finance.OperationsRequireModeration.ShouldBeTrue();
            // Операция ждёт решения мастера, поэтому в оплаченное она не попадает.
            claim.Finance.FeePaid.ShouldBe(0);
            claim.PlayerAllowedSensitiveData.ShouldBeTrue();
        }
    }
}
