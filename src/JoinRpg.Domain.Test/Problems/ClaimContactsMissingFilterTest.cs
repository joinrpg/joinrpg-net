using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain.Problems.ClaimProblemFilters;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Domain.Test.Problems;

/// <summary>
/// Фильтр «в профиле не хватает контактов» поверх доменных сущностей (ADR013).
/// </summary>
/// <remarks>
/// Раньше фильтр читал контакты прямо из EF-полей <c>claim.Player.Extra</c>, и тест подменял их
/// там же. Теперь профиль приходит готовым <see cref="UserInfo"/>, поэтому случаи задаются
/// значениями этой записи — набор проверяемых случаев тот же.
/// </remarks>
public class ClaimContactsMissingFilterTest
{
    private MockedProject Mock { get; } = new MockedProject();
    private ClaimContactsMissingFilter Filter { get; } = new ClaimContactsMissingFilter();

    private ProjectInfo WithRequirement(Func<ProjectProfileRequirementSettings, ProjectProfileRequirementSettings> setup)
        => Mock.ProjectInfo.WithProfileRequirementSettings(setup(ProjectProfileRequirementSettings.AllNotRequired));

    /// <param name="player">
    /// Профиль игрока; по умолчанию — <c>MockedProject.PlayerInfo</c>, в котором из контактов не
    /// заполнено ничего.
    /// </param>
    private ClaimInfo MakeContext(
        ProjectInfo projectInfo,
        bool sensitiveDataAllowed = false,
        UserInfo? player = null)
    {
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);
        claim.PlayerAllowedSenstiveData = sensitiveDataAllowed;

        // Агрегат собирается на тех же метаданных, что проверяет тест: конструктор CharacterInfo
        // требует ровно тот экземпляр ProjectInfo, к которому привязаны слои полей, а
        // WithProfileRequirementSettings возвращает новый.
        var character = Mock.GetCharacterInfo(Mock.Character, projectInfo);

        return new ClaimInfo(new ClaimInCharacter(character, claim.GetId()), player ?? Mock.PlayerInfo);
    }

    [Fact]
    public void ToClaimProblemTypeIsDefinedForEveryUserProfileItemType()
    {
        foreach (var itemType in Enum.GetValues<UserProfileItemType>())
        {
            Should.NotThrow(() => ClaimContactsMissingFilter.ToClaimProblemType(itemType));
        }
    }

    [Fact]
    public void PhoneMissingWhenNotFilledAtAll()
    {
        var projectInfo = WithRequirement(s => s with { RequirePhone = MandatoryStatus.Required });

        Filter.GetProblems(MakeContext(projectInfo))
            .ShouldContain(p => p.ProblemType == ClaimProblemType.MissingPhone);
    }

    [Fact]
    public void PhoneMissingWhenTooShort()
    {
        var projectInfo = WithRequirement(s => s with { RequirePhone = MandatoryStatus.Required });
        var player = Mock.PlayerInfo with { PhoneNumber = "123" };

        Filter.GetProblems(MakeContext(projectInfo, player: player))
            .ShouldContain(p => p.ProblemType == ClaimProblemType.MissingPhone);
    }

    [Fact]
    public void PhoneNotMissingWhenFilledCorrectly()
    {
        var projectInfo = WithRequirement(s => s with { RequirePhone = MandatoryStatus.Required });
        var player = Mock.PlayerInfo with { PhoneNumber = "+79991234567" };

        Filter.GetProblems(MakeContext(projectInfo, player: player))
            .ShouldNotContain(p => p.ProblemType == ClaimProblemType.MissingPhone);
    }

    [Fact]
    public void RealNameMissingWhenNotFilledAtAll()
    {
        var projectInfo = WithRequirement(s => s with { RequireRealName = MandatoryStatus.Required });

        Filter.GetProblems(MakeContext(projectInfo))
            .ShouldContain(p => p.ProblemType == ClaimProblemType.MissingRealname);
    }

    [Fact]
    public void RealNameNotMissingWhenFilledCorrectly()
    {
        var projectInfo = WithRequirement(s => s with { RequireRealName = MandatoryStatus.Required });
        var player = Mock.PlayerInfo with
        {
            UserFullName = new UserFullName(
                PrefferedName.FromOptional("Player"),
                BornName.FromOptional("Иван"),
                SurName.FromOptional("Иванов"),
                FatherName: null),
        };

        Filter.GetProblems(MakeContext(projectInfo, player: player))
            .ShouldNotContain(p => p.ProblemType == ClaimProblemType.MissingRealname);
    }

    [Fact]
    public void PassportMissingWhenAllowedButNotFilled()
    {
        var projectInfo = WithRequirement(s => s with { RequirePassport = MandatoryStatus.Required });

        Filter.GetProblems(MakeContext(projectInfo, sensitiveDataAllowed: true))
            .ShouldContain(p => p.ProblemType == ClaimProblemType.MissingPassport);
    }

    [Fact]
    public void PassportNotMissingWhenAllowedAndFilled()
    {
        var projectInfo = WithRequirement(s => s with { RequirePassport = MandatoryStatus.Required });
        var player = Mock.PlayerInfo with { PassportData = "1234 567890" };

        Filter.GetProblems(MakeContext(projectInfo, sensitiveDataAllowed: true, player: player))
            .ShouldNotContain(p => p.ProblemType == ClaimProblemType.MissingPassport);
    }

    [Fact]
    public void RegistrationAddressMissingWhenAllowedButNotFilled()
    {
        var projectInfo = WithRequirement(s => s with { RequireRegistrationAddress = MandatoryStatus.Required });

        Filter.GetProblems(MakeContext(projectInfo, sensitiveDataAllowed: true))
            .ShouldContain(p => p.ProblemType == ClaimProblemType.MissingRegistrationAddress);
    }

    [Fact]
    public void SensitiveDataNotAllowedWhenConsentNotGiven()
    {
        var projectInfo = WithRequirement(s => s with { RequirePassport = MandatoryStatus.Required });

        var problems = Filter.GetProblems(MakeContext(projectInfo, sensitiveDataAllowed: false));

        problems.ShouldContain(p => p.ProblemType == ClaimProblemType.SensitiveDataNotAllowed);
        problems.ShouldNotContain(p => p.ProblemType == ClaimProblemType.MissingPassport);
        problems.ShouldNotContain(p => p.ProblemType == ClaimProblemType.MissingRegistrationAddress);
    }

    [Fact]
    public void NoSensitiveDataProblemsWhenNotRequiredAtAllRegardlessOfConsent()
    {
        var projectInfo = WithRequirement(s => s);

        var problems = Filter.GetProblems(MakeContext(projectInfo, sensitiveDataAllowed: false));

        problems.ShouldNotContain(p => p.ProblemType == ClaimProblemType.SensitiveDataNotAllowed);
        problems.ShouldNotContain(p => p.ProblemType == ClaimProblemType.MissingPassport);
        problems.ShouldNotContain(p => p.ProblemType == ClaimProblemType.MissingRegistrationAddress);
    }

    /// <summary>
    /// Переход на UserProfileProblemsCalculator изменил смысл «ВК заполнен»: теперь это
    /// привязка ExternalLogin плюс флаг верификации, а не флаг рядом с легаси-строкой.
    /// </summary>
    /// <remarks>
    /// У игрока, которому ВК привязали до перехода на нынешний стек авторизации, в профиле есть
    /// UserExtra.Vk и VkVerified, но записи в UserExternalLogins нет — и проблема теперь
    /// появляется там, где её не было. Это осознанная цена согласованности: правила подачи
    /// заявки (ClaimValidator.ValidateContacts) считали ВК ровно так уже до этого коммита, то
    /// есть «нельзя подать заявку: нет ВК» и «в заявке проблема: нет ВК» расходились.
    /// </remarks>
    [Fact]
    public void VkontakteFromLegacyFieldOnlyIsNotVerified()
    {
        var projectInfo = WithRequirement(s => s with { RequireVkontakte = MandatoryStatus.Required });
        var player = WithSocial(vk: VkSocialLink.FromUserData(externalLoginKey: null, legacyVk: "durov", vkVerified: true));

        Filter.GetProblems(MakeContext(projectInfo, player: player))
            .ShouldContain(p => p.ProblemType == ClaimProblemType.MissingVkontakte);
    }

    [Fact]
    public void VkontakteWithExternalLoginIsVerified()
    {
        var projectInfo = WithRequirement(s => s with { RequireVkontakte = MandatoryStatus.Required });
        var player = WithSocial(vk: VkSocialLink.FromUserData(externalLoginKey: "1", legacyVk: null, vkVerified: true));

        Filter.GetProblems(MakeContext(projectInfo, player: player))
            .ShouldNotContain(p => p.ProblemType == ClaimProblemType.MissingVkontakte);
    }

    /// <summary>
    /// Зеркальная сторона того же перехода: привязанный телеграм без @username раньше считался
    /// незаполненным, потому что легаси-поле UserExtra.Telegram оставалось пустым.
    /// </summary>
    [Fact]
    public void TelegramWithExternalLoginButWithoutUserNameIsFilled()
    {
        var projectInfo = WithRequirement(s => s with { RequireTelegram = MandatoryStatus.Required });
        var player = WithSocial(telegram: TelegramSocialLink.FromUserData(externalLoginKey: "123", prettyName: null));

        Filter.GetProblems(MakeContext(projectInfo, player: player))
            .ShouldNotContain(p => p.ProblemType == ClaimProblemType.MissingTelegram);
    }

    [Fact]
    public void TelegramMissingWhenNothingLinked()
    {
        var projectInfo = WithRequirement(s => s with { RequireTelegram = MandatoryStatus.Required });

        Filter.GetProblems(MakeContext(projectInfo))
            .ShouldContain(p => p.ProblemType == ClaimProblemType.MissingTelegram);
    }

    private UserInfo WithSocial(TelegramSocialLink? telegram = null, VkSocialLink? vk = null)
        => Mock.PlayerInfo with
        {
            Social = Mock.PlayerInfo.Social with { Telegram = telegram, Vk = vk },
        };
}
