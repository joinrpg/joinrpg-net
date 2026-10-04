using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test.Problems;

/// <summary>
/// Общая оснастка тестов фильтров проблем заявки: собирает <see cref="ClaimProblemContext"/>, в
/// котором можно точечно подменить и персонажа, и заявку.
/// </summary>
/// <remarks>
/// Заявка берётся из агрегата, собранного моком (<c>MockedProject.GetCharacterInfo</c>), и
/// правится через <c>with</c>. Так тест задаёт ровно те поля, которые различает проверяемое
/// правило, и не зависит от того, что именно мок умеет положить в EF-сущность: часть полей
/// доменной заявки (например <c>FeePaid</c>) мок не выводит из сущности вовсе.
/// </remarks>
public abstract class ClaimProblemFilterTestBase
{
    protected MockedProject Mock { get; } = new MockedProject();

    /// <summary>
    /// Контекст с одной заявкой на персонаже мока.
    /// </summary>
    /// <param name="setupClaim">Что поменять в доменной заявке относительно собранной моком.</param>
    /// <param name="projectInfo">Метаданные проекта; по умолчанию — метаданные мока.</param>
    /// <param name="characterIsActive">Персонаж не удалён. Нужно правилам «сломанная заявка».</param>
    /// <param name="characterHasOtherApprovedClaim">
    /// У персонажа есть утверждённая заявка, и это НЕ та, которую проверяем. Собирается как вторая
    /// заявка, потому что инварианты агрегата не дают сослаться на несуществующую.
    /// </param>
    protected ClaimProblemContext MakeContext(
        Func<CharacterClaimInfo, CharacterClaimInfo>? setupClaim = null,
        ProjectInfo? projectInfo = null,
        bool characterIsActive = true,
        bool characterHasOtherApprovedClaim = false)
    {
        projectInfo ??= Mock.ProjectInfo;

        _ = Mock.CreateClaim(Mock.Character, Mock.Player);
        var template = Mock.GetCharacterInfo(Mock.Character, projectInfo).Claims.First();

        var claim = setupClaim is null ? template : setupClaim(template);

        var claims = new List<CharacterClaimInfo> { claim };
        ClaimIdentification? approvedClaimId = claim.IsApproved ? claim.ClaimId : null;

        if (characterHasOtherApprovedClaim)
        {
            var otherApproved = template with
            {
                ClaimId = new ClaimIdentification(claim.ClaimId.ProjectId, claim.ClaimId.ClaimId + 1000),
                Status = ClaimStatus.Approved,
            };
            claims.Add(otherApproved);
            approvedClaimId = otherApproved.ClaimId;
        }

        var character = MakeCharacter(projectInfo, claims, approvedClaimId, characterIsActive);

        return new ClaimProblemContext(character, claim, Mock.PlayerInfo);
    }

    /// <summary>
    /// Агрегат персонажа мока с заданным набором заявок. Собирается конструктором, а не
    /// <c>with</c>: у <see cref="CharacterInfo"/> нет init-свойств, зато в конструкторе живут
    /// инварианты, и пройти их — часть проверки.
    /// </summary>
    private CharacterInfo MakeCharacter(
        ProjectInfo projectInfo,
        IReadOnlyCollection<CharacterClaimInfo> claims,
        ClaimIdentification? approvedClaimId,
        bool isActive)
    {
        var source = Mock.GetCharacterInfo(Mock.Character, projectInfo);

        return new CharacterInfo(
            source.Id,
            projectInfo,
            source.CharacterName,
            source.CharacterTypeInfo,
            source.HidePlayerForCharacter,
            isActive,
            source.InGame,
            source.AutoCreated,
            source.Description,
            source.OriginalCharacterSlotId,
            source.DirectGroupIds,
            source.CharacterFields,
            source.PlotElementOrderData,
            claims,
            approvedClaimId,
            source.CreatedAt,
            source.CreatedById,
            source.UpdatedAt,
            source.UpdatedById);
    }
}
