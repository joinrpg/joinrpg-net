using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain.Access;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test.Access;

public class AccessArgumentsFactoryTest
{
    private MockedProject Mock { get; } = new MockedProject();

    private static UserIdentification MasterUser => new UserIdentification(2);
    private static UserIdentification PlayerUser => new UserIdentification(1);

    [Fact]
    public void HiddenCharacter_AnonymousUser_CannotView()
    {
        // Персонаж не публичный, пользователь не авторизован
        Mock.Character.IsPublic = false;
        var args = AccessArgumentsFactory.Create(Mock.Character, (UserIdentification?)null, Mock.ProjectInfo);
        args.CanViewCharacterAtAll.ShouldBeFalse();
    }

    [Fact]
    public void HiddenCharacter_MasterUser_CanView()
    {
        // Персонаж не публичный, но пользователь — мастер проекта
        Mock.Character.IsPublic = false;
        var args = AccessArgumentsFactory.Create(Mock.Character, MasterUser, Mock.ProjectInfo);
        args.CanViewCharacterAtAll.ShouldBeTrue();
    }

    [Fact]
    public void HiddenCharacter_ApprovedPlayer_CanView()
    {
        // Персонаж не публичный, но пользователь — утверждённый игрок
        Mock.Character.IsPublic = false;
        _ = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);
        var args = AccessArgumentsFactory.Create(Mock.Character, PlayerUser, Mock.ProjectInfo);
        args.CanViewCharacterAtAll.ShouldBeTrue();
    }

    [Fact]
    public void PublicCharacter_AnonymousUser_CanView()
    {
        // Персонаж публичный — виден всем
        Mock.Character.IsPublic = true;
        var args = AccessArgumentsFactory.Create(Mock.Character, (UserIdentification?)null, Mock.ProjectInfo);
        args.CanViewCharacterAtAll.ShouldBeTrue();
    }

    [Fact]
    public void HiddenCharacter_PublishedProject_AnonCanView()
    {
        // Опубликованные вводные открывают все персонажи для чтения
        Mock.Character.IsPublic = false;
        Mock.Project.Details.PublishPlot = true;
        Mock.ReInitProjectInfo();
        var args = AccessArgumentsFactory.Create(Mock.Character, (UserIdentification?)null, Mock.ProjectInfo);
        args.CanViewCharacterAtAll.ShouldBeTrue();
    }

    [Fact]
    public void HiddenCharacter_OtherPlayer_CannotView()
    {
        // Чужой игрок не должен видеть скрытого персонажа другого игрока
        Mock.Character.IsPublic = false;
        _ = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);
        var otherPlayer = new UserIdentification(99);
        var args = AccessArgumentsFactory.Create(Mock.Character, otherPlayer, Mock.ProjectInfo);
        args.CanViewCharacterAtAll.ShouldBeFalse();
    }

    #region Права при сохранении полей: (CharacterInfo, CharacterClaimInfo?)

    [Fact]
    public void SavingFields_ApprovedClaimNotYetInAggregate_PlayerKeepsCharacterAccess()
    {
        // Главный случай ради которого перегрузка и заведена: при принятии заявки мастером
        // статус и ApprovedClaimId проставлены в памяти, но агрегат грузится из БД и о них
        // не знает. Без учёта сохраняемой заявки игрок потерял бы доступ к своему персонажу.
        var character = MakeCharacterInfo();
        var approvedClaim = MakeClaimInfo(ClaimStatus.Approved, PlayerUser);

        var args = AccessArgumentsFactory.Create(character, approvedClaim, PlayerUser);

        args.PlayerAccessToCharacter.ShouldBeTrue();
        args.PlayerAccesToClaim.ShouldBeTrue();
    }

    [Fact]
    public void SavingFields_UnapprovedClaim_GivesClaimAccessButNotCharacterAccess()
    {
        var character = MakeCharacterInfo();
        var claim = MakeClaimInfo(ClaimStatus.AddedByUser, PlayerUser);

        var args = AccessArgumentsFactory.Create(character, claim, PlayerUser);

        args.PlayerAccesToClaim.ShouldBeTrue();
        args.PlayerAccessToCharacter.ShouldBeFalse();
    }

    [Fact]
    public void SavingFields_ForeignApprovedClaim_GivesNoAccess()
    {
        var character = MakeCharacterInfo();
        var claim = MakeClaimInfo(ClaimStatus.Approved, new UserIdentification(99));

        var args = AccessArgumentsFactory.Create(character, claim, PlayerUser);

        args.PlayerAccessToCharacter.ShouldBeFalse();
        args.PlayerAccesToClaim.ShouldBeFalse();
    }

    [Fact]
    public void SavingFields_WithoutClaim_MatchesCharacterOnlyRule()
    {
        // Без заявки правило то же, что у перегрузки по одному персонажу.
        var character = MakeCharacterInfo();

        var args = AccessArgumentsFactory.Create(character, claim: null, PlayerUser);

        args.ShouldBe(AccessArgumentsFactory.Create(character, PlayerUser));
    }

    [Fact]
    public void SavingFields_Master_HasMasterAccess()
    {
        var character = MakeCharacterInfo();

        var args = AccessArgumentsFactory.Create(character, claim: null, MasterUser);

        args.MasterAccess.ShouldBeTrue();
    }

    private CharacterInfo MakeCharacterInfo()
        => CharacterInfo.ForNewCharacter(
            new CharacterIdentification(Mock.ProjectInfo.ProjectId, Mock.Character.CharacterId),
            Mock.ProjectInfo,
            Mock.Character.CharacterName,
            CharacterTypeInfo.Default(),
            hidePlayerForCharacter: false,
            [Mock.ProjectInfo.RootCharacterGroupId],
            FieldLayerContainer.Empty(Mock.ProjectInfo),
            new UserIdentification(Mock.Master.UserId),
            DateTime.UtcNow);

    private CharacterClaimInfo MakeClaimInfo(ClaimStatus status, UserIdentification playerId)
        => new(
            new ClaimIdentification(Mock.ProjectInfo.ProjectId, -1),
            new UserInfoHeader(playerId, new UserDisplayName("Игрок", null)),
            status,
            DenialStatus: null,
            ResponsibleMasterId: new UserIdentification(Mock.Master.UserId),
            CreateDate: DateTime.UtcNow,
            LastUpdateDateTime: DateTime.UtcNow,
            CheckInDate: null,
            LastPlayerCommentAt: null,
            LastMasterCommentAt: null,
            LastVisibleMasterCommentAt: null,
            CurrentFee: null,
            PreferentialFeeUser: false,
            FeePaid: 0,
            AccommodationFee: 0,
            Fields: FieldLayerContainer.Empty(Mock.ProjectInfo));

    #endregion
}
