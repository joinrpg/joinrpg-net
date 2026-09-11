using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test.AddClaim;

public class AddClaimValidationRulesTest
{
    private MockedProject Mock { get; } = new MockedProject();

    [Fact]
    public void AddClaimAllowedCharacter() => ShouldBeAllowed(Mock.Character, Mock.ProjectInfo);

    [Fact]
    public void CantSentClaimToInactiveCharacter()
    {
        var inactive = Mock.CreateCharacter("inactive");
        inactive.IsActive = false;
        ShouldBeNotAllowed(inactive, AddClaimForbideReason.CharacterInactive, Mock.ProjectInfo);
    }

    [Fact]
    public void AddClaimAllowedCharacterWithoutUser() => Mock.Character.ValidateIfCanAddClaim(userInfo: null, Mock.ProjectInfo, ClaimOperation.AddByPlayer).ShouldBeEmpty();

    [Fact]
    public void CantSendClaimIfProjectClaimsClosed()
    {
        var projectInfo = Mock.ProjectInfo.WithChangedStatus(ProjectLifecycleStatus.ActiveClaimsClosed);
        ShouldBeNotAllowed(Mock.Character, AddClaimForbideReason.ProjectClaimsClosed, projectInfo);
    }

    [Fact]
    public void CantSendClaimIfProjectClosed()
    {
        var projectInfo = Mock.ProjectInfo.WithChangedStatus(ProjectLifecycleStatus.Archived);
        ShouldBeNotAllowed(Mock.Character, AddClaimForbideReason.ProjectNotActive, projectInfo);
    }

    [Fact]
    public void CantSendClaimIfNoSlotsChar()
    {
        Mock.Character.CharacterType = CharacterType.Slot;
        Mock.Character.CharacterSlotLimit = 0;
        ShouldBeNotAllowed(Mock.Character, AddClaimForbideReason.SlotsExhausted, Mock.ProjectInfo);
    }


    [Fact]
    public void CantSendClaimIfCharacterIsNpc()
    {
        Mock.Character.CharacterType = CharacterType.NonPlayer;
        ShouldBeNotAllowed(Mock.Character, AddClaimForbideReason.Npc, Mock.ProjectInfo);
    }

    [Fact]
    public void CantSendClaimIfCharacterHasApprovedClaim()
    {
        // Именно настоящая заявка, а не ApprovedClaimId = -1: минус первый — это сентинел
        // «заявки нет» (см. ClaimIdentification.FromOptional), то есть ровно обратный смысл.
        _ = Mock.CreateApprovedClaim(Mock.Character, Mock.Master);
        ShouldBeNotAllowed(Mock.Character, AddClaimForbideReason.Busy, Mock.ProjectInfo);
    }

    [Fact]
    public void CantSendClaimIfCharacterHasCheckedInClaim()
    {
        _ = Mock.CreateCheckedInClaim(Mock.Character, Mock.Master);
        ShouldBeNotAllowed(Mock.Character, AddClaimForbideReason.Busy, Mock.ProjectInfo);
    }

    [Fact]
    public void CantSendClaimToSameCharacter()
    {
        _ = Mock.CreateClaim(Mock.Character, Mock.Player);
        ShouldBeNotAllowed(Mock.Character, AddClaimForbideReason.AlreadySent, Mock.ProjectInfo);
    }

    [Fact]
    public void CantSendClaimToSameCharacterEvenProjectSettingsAllowsMultiple()
    {
        Mock.Project.Details.EnableManyCharacters = true;
        _ = Mock.CreateClaim(Mock.Character, Mock.Player);
        ShouldBeNotAllowed(Mock.Character, AddClaimForbideReason.AlreadySent, Mock.ProjectInfo);
    }

    [Fact]
    public void CantSendClaimIfHasApproved()
    {
        _ = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);
        var another = Mock.CreateCharacter("another");
        ShouldBeNotAllowed(another, AddClaimForbideReason.OnlyOneCharacter, Mock.ProjectInfo);
    }

    [Fact]
    public void AllowSendClaimEvenIfHasApprovedAccordingToSettings()
    {
        var projectInfo = Mock.ProjectInfo.WithAllowManyClaims(strictlyOneCharacter: false);
        var
        _ = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);
        var another = Mock.CreateCharacter("another");
        ShouldBeAllowed(another, projectInfo);
    }

    [Fact]
    public void AllowSendClaimEvenIfHasAnotherNotApproved()
    {
        _ = Mock.CreateClaim(Mock.Character, Mock.Player);
        var another = Mock.CreateCharacter("another");
        ShouldBeAllowed(another, Mock.ProjectInfo);
    }

    [Fact]
    public void CantSendClaimIfVkRequiredButNotVerified()
    {
        var projectInfo = Mock.ProjectInfo.WithProfileRequirementSettings(
            ProjectProfileRequirementSettings.AllNotRequired with { RequireVkontakte = MandatoryStatus.Required });
        var playerWithUnverifiedVk = Mock.PlayerInfo with
        {
            Social = Mock.PlayerInfo.Social with { Vk = new VkSocialLink(1, isVerified: false) },
        };
        Mock.Character.ValidateIfCanAddClaim(playerWithUnverifiedVk, projectInfo, ClaimOperation.AddByPlayer).Kinds()
            .ShouldContain(AddClaimForbideReason.VkontakteMissing);
    }

    [Fact]
    public void CanSendClaimIfVkRequiredAndVerified()
    {
        var projectInfo = Mock.ProjectInfo.WithProfileRequirementSettings(
            ProjectProfileRequirementSettings.AllNotRequired with { RequireVkontakte = MandatoryStatus.Required });
        var playerWithVerifiedVk = Mock.PlayerInfo with
        {
            Social = Mock.PlayerInfo.Social with { Vk = new VkSocialLink(1, isVerified: true) },
        };
        Mock.Character.ValidateIfCanAddClaim(playerWithVerifiedVk, projectInfo, ClaimOperation.AddByPlayer).ShouldBeEmpty();
    }

    [Fact]
    public void CantSendClaimIfPhoneRequiredButMissing()
    {
        var projectInfo = Mock.ProjectInfo.WithProfileRequirementSettings(
            ProjectProfileRequirementSettings.AllNotRequired with { RequirePhone = MandatoryStatus.Required });
        Mock.Character.ValidateIfCanAddClaim(Mock.PlayerInfo, projectInfo, ClaimOperation.AddByPlayer).Kinds()
            .ShouldContain(AddClaimForbideReason.PhoneMissing);
    }

    [Fact]
    public void CanSendClaimIfPhoneRequiredAndFilled()
    {
        var projectInfo = Mock.ProjectInfo.WithProfileRequirementSettings(
            ProjectProfileRequirementSettings.AllNotRequired with { RequirePhone = MandatoryStatus.Required });
        var playerWithPhone = Mock.PlayerInfo with { PhoneNumber = "+79991234567" };
        Mock.Character.ValidateIfCanAddClaim(playerWithPhone, projectInfo, ClaimOperation.AddByPlayer).ShouldBeEmpty();
    }

    [Fact]
    public void CantSendClaimIfRealNameRequiredButMissing()
    {
        var projectInfo = Mock.ProjectInfo.WithProfileRequirementSettings(
            ProjectProfileRequirementSettings.AllNotRequired with { RequireRealName = MandatoryStatus.Required });
        Mock.Character.ValidateIfCanAddClaim(Mock.PlayerInfo, projectInfo, ClaimOperation.AddByPlayer).Kinds()
            .ShouldContain(AddClaimForbideReason.RealNameMissing);
    }

    [Fact]
    public void CanSendClaimIfRealNameRequiredAndFilled()
    {
        var projectInfo = Mock.ProjectInfo.WithProfileRequirementSettings(
            ProjectProfileRequirementSettings.AllNotRequired with { RequireRealName = MandatoryStatus.Required });
        var playerWithRealName = Mock.PlayerInfo with
        {
            UserFullName = new UserFullName(new PrefferedName("Player"), new BornName("Иван"), new SurName("Иванов"), null),
        };
        Mock.Character.ValidateIfCanAddClaim(playerWithRealName, projectInfo, ClaimOperation.AddByPlayer).ShouldBeEmpty();
    }

    private void ShouldBeAllowed(Character mockCharacter, ProjectInfo projectInfo)
        => mockCharacter.ValidateIfCanAddClaim(Mock.PlayerInfo, projectInfo, ClaimOperation.AddByPlayer).ShouldBeEmpty();

    private void ShouldBeNotAllowed(Character claimSource, AddClaimForbideReason reason, ProjectInfo projectInfo)
    {
        claimSource.ValidateIfCanAddClaim(Mock.PlayerInfo, projectInfo, ClaimOperation.AddByPlayer).Kinds().ShouldContain(reason);
    }
}
