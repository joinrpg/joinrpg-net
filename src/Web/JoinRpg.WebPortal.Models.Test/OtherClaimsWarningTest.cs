using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Web.ProjectCommon.Claims;

namespace JoinRpg.WebPortal.Models.Test;

/// <summary>
/// Перечень заявок для предупреждений страницы заявки (#6089): до 10 ссылок, остальные числом.
/// </summary>
public class OtherClaimsWarningTest
{
    private static readonly ProjectIdentification ProjectId = new(1);
    private static readonly ClaimIdentification CurrentClaimId = new(ProjectId, 3);

    [Fact]
    public void Create_ShowsNoMoreThanTenClaims()
    {
        var warning = OtherClaimsWarning.Create(CreateLinks(12));

        warning.DisplayedClaims.Count.ShouldBe(OtherClaimsWarning.MaxDisplayedClaims);
        warning.HiddenClaimsCount.ShouldBe(2);
        warning.HasClaims.ShouldBeTrue();
    }

    [Fact]
    public void Create_LimitExact_CountsNothingHidden()
    {
        var warning = OtherClaimsWarning.Create(CreateLinks(OtherClaimsWarning.MaxDisplayedClaims));

        warning.HiddenClaimsCount.ShouldBe(0);
    }

    [Fact]
    public void Empty_HasNoClaims()
    {
        OtherClaimsWarning.Empty.HasClaims.ShouldBeFalse();
    }

    [Fact]
    public void ForApproveClaim_SkipsCurrentClaim_AndOrdersByClaimId()
    {
        var headers = new[]
        {
            CreateHeader(5, "Фродо"),
            CreateHeader(2, "Гэндальф"),
            CreateHeader(CurrentClaimId.ClaimId, "Текущая"),
        };

        var warning = OtherClaimsWarning.ForApproveClaim(headers, CurrentClaimId, acceptingDeclinesOtherClaims: true);

        warning.DisplayedClaims.Select(claim => claim.CharacterName).ShouldBe(["Гэндальф", "Фродо"]);
    }

    [Fact]
    public void ForApproveClaim_NotRelevant_IfAcceptingDeclinesNothing()
    {
        var warning = OtherClaimsWarning.ForApproveClaim(
            [CreateHeader(1, "Гэндальф")], CurrentClaimId, acceptingDeclinesOtherClaims: false);

        warning.HasClaims.ShouldBeFalse();
    }

    [Fact]
    public void ForRestoreClaim_ListsOnlyApprovedClaims()
    {
        var headers = new[]
        {
            CreateHeader(1, "Гэндальф"),
            CreateHeader(2, "Обсуждаемая"),
            CreateHeader(4, "Арагорн"),
        };
        var playerActiveClaims = new[]
        {
            new UserClaimInfo(new ClaimIdentification(ProjectId, 1), ClaimStatus.Approved),
            new UserClaimInfo(new ClaimIdentification(ProjectId, 2), ClaimStatus.Discussed),
            new UserClaimInfo(new ClaimIdentification(ProjectId, 4), ClaimStatus.CheckedIn),
        };

        var warning = OtherClaimsWarning.ForRestoreClaim(headers, CurrentClaimId, playerActiveClaims, strictlyOneCharacter: true);

        warning.DisplayedClaims.Select(claim => claim.CharacterName).ShouldBe(["Гэндальф", "Арагорн"]);
    }

    [Fact]
    public void ForRestoreClaim_MultipleCharactersAllowed_NothingBlocksRestore()
    {
        var headers = new[] { CreateHeader(1, "Гэндальф") };
        var playerActiveClaims = new[] { new UserClaimInfo(new ClaimIdentification(ProjectId, 1), ClaimStatus.Approved) };

        var warning = OtherClaimsWarning.ForRestoreClaim(headers, CurrentClaimId, playerActiveClaims, strictlyOneCharacter: false);

        warning.HasClaims.ShouldBeFalse();
    }

    [Fact]
    public void ForRestoreClaim_ApprovedCurrentClaimDoesNotBlockItself()
    {
        var headers = new[] { CreateHeader(CurrentClaimId.ClaimId, "Текущая"), CreateHeader(7, "Гэндальф") };
        var playerActiveClaims = new[]
        {
            new UserClaimInfo(CurrentClaimId, ClaimStatus.Approved),
            new UserClaimInfo(new ClaimIdentification(ProjectId, 7), ClaimStatus.Approved),
        };

        var warning = OtherClaimsWarning.ForRestoreClaim(headers, CurrentClaimId, playerActiveClaims, strictlyOneCharacter: true);

        warning.DisplayedClaims.Select(claim => claim.CharacterName).ShouldBe(["Гэндальф"]);
    }

    private static IReadOnlyCollection<ClaimLinkViewModel> CreateLinks(int count)
        => [.. Enumerable.Range(1, count).Select(i => CreateLink(i, "Персонаж " + i))];

    private static ClaimLinkViewModel CreateLink(int claimId, string characterName)
        => new(new ClaimIdentification(ProjectId, claimId), new UserDisplayName("Игрок", null), characterName, "", new UserIdentification(2));

    private static ClaimWithPlayer CreateHeader(int claimId, string characterName)
        => new()
        {
            ClaimId = new ClaimIdentification(ProjectId, claimId),
            CharacterName = characterName,
            Player = new UserInfoHeader(
                new UserIdentification(2),
                new UserDisplayName(new UserFullName(new PrefferedName("Игрок"), null, null, null), new Email("player@example.com")),
                null),
            ExtraNicknames = null,
            ResponsibleMasterUserId = new UserIdentification(3),
            CharacterId = new CharacterIdentification(ProjectId, 4),
        };
}
