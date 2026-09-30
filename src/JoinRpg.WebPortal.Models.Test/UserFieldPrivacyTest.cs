using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Common.WebComponents;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain.Access;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.WebPortal.Models.Test;

/// <summary>
/// Инвариант приватности поля-ссылки на пользователя — ADR017 §6 (docs/adr017-user-field.md).
/// </summary>
/// <remarks>
/// <para>
/// Ссылка на человека — персональные данные. Упоминание пользователя в поле проекта даёт ровно
/// две вещи: отображаемое имя и ссылку на публичный профиль. Оно <b>не</b> даёт смотрящему
/// доступ к контактам упомянутого и <b>не</b> даёт самому упомянутому никакого отношения
/// к проекту.
/// </para>
/// <para>
/// Тест намеренно написан так, чтобы сломаться, если кто-то потом протащит контакты в показ
/// user-поля: набор свойств <see cref="UserLinkViewModel"/> проверяется целиком, а не выборочно.
/// Если инвариант решено менять — менять его надо в ADR017, а не подкручивать тест.
/// </para>
/// </remarks>
public class UserFieldPrivacyTest
{
    /// <summary>Посторонний, которого мастер вписал в поле: контакты заполнены, к проекту отношения нет.</summary>
    private static readonly UserInfo MentionedUser = new(
        new UserIdentification(7),
        Social: new UserSocialNetworks(
            new TelegramSocialLink(new TelegramChatId(77777), new PrefferedName("secret_telegram")),
            LiveJournal: null,
            AllrpgInfoId: null,
            Vk: new VkSocialLink(777, new PrefferedName("secret_vk"), isVerified: true),
            ContactsAccessType.Public),
        ActiveClaims: [],
        ActiveProjects: [],
        AllProjects: [],
        IsAdmin: false,
        SelectedAvatarId: null,
        new Email("secret@example.com"),
        EmailConfirmed: true,
        new UserFullName(new PrefferedName("Седьмой"), null, null, null),
        VerifiedProfileFlag: false,
        PhoneNumber: "+7 900 000-00-00",
        HasPassword: false);

    /// <summary>
    /// ADR017 §6: упоминание в поле не входит в расчёт <see cref="UserInfo.GetAccess(UserInfo?)"/>
    /// и входить не должно. Мастер проекта, открывший поле со ссылкой, контактов упомянутого
    /// не получает.
    /// </summary>
    [Fact]
    public void MentionedUserGivesNoContactsAccessToMaster()
    {
        var mock = new MockedProject();
        MentionField(mock, MentionedUser.UserId);

        // Мастер именно этого проекта — то есть тот, кто ссылку и видит.
        var master = mock.MasterInfo with { ActiveProjects = [mock.ProjectInfo.ProjectId] };

        MentionedUser.GetAccess(master).ShouldBe(UserProfileAccessReason.NoAccess);

        // Контроль: сам механизм доступа работает — у игрока с активной заявкой мастер контакты видит.
        // Значит NoAccess выше получен из-за отсутствия заявки, а не из-за сломанного теста.
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        claim.ClaimStatus = ClaimStatus.Approved;
        mock.PlayerInfo.GetAccess(master).ShouldBe(UserProfileAccessReason.Master);
    }

    /// <summary>
    /// ADR017 §6: упоминание не даёт упомянутому никакого доступа к проекту — ни как игроку,
    /// ни как со-мастеру.
    /// </summary>
    [Fact]
    public void MentionedUserGetsNoAccessToProject()
    {
        var mock = new MockedProject();
        MentionField(mock, MentionedUser.UserId);

        MentionedUser.GetAccess(mock.ProjectInfo).ShouldBe(UserProfileAccessReason.NoAccess);
    }

    /// <summary>
    /// ADR017 §6: во вьюмодель поля попадают только идентификатор и отображаемое имя.
    /// Ни email, ни телефон, ни соцсети упомянутого в показ поля не уезжают.
    /// </summary>
    [Fact]
    public void UserFieldViewModelExposesOnlyIdAndDisplayName()
    {
        var mock = new MockedProject();
        var field = MentionField(mock, MentionedUser.UserId);

        IReadOnlyDictionary<UserIdentification, UserInfoHeader> users =
            new Dictionary<UserIdentification, UserInfoHeader>
            {
                // В резолв уходит заведомо UserInfoHeader, а не UserInfo: другого пути к показу нет.
                [MentionedUser.UserId] = MentionedUser.ToUserInfoHeader(),
            };

        var vm = new CustomFieldsViewModel(
            mock.Character,
            mock.ProjectInfo,
            AccessArgumentsFactory.Create(mock.Character, new UserIdentification(mock.Master.UserId), mock.ProjectInfo),
            users);

        var userLink = vm.Field(field).ShouldNotBeNull().UserLinks.ShouldHaveSingleItem();

        userLink.UserId.ShouldBe(MentionedUser.UserId);
        userLink.DisplayName.ShouldBe(MentionedUser.DisplayName.DisplayName);

        // Весь публичный состав модели ссылки — целиком. Новое свойство (Email, PhoneNumber,
        // Social, «а давайте ещё UserInfo целиком») уронит этот тест, и это ровно то, чего мы
        // хотим: ADR017 §6 разрешает показывать только имя и ссылку на профиль.
        typeof(UserLinkViewModel)
            .GetProperties()
            .Select(p => p.Name)
            .ShouldBe(["UserId", "DisplayName", "ViewMode"], ignoreOrder: true);
    }

    /// <summary>Заводит в проекте user-поле и вписывает туда указанного пользователя.</summary>
    private static ProjectFieldInfo MentionField(MockedProject mock, UserIdentification userId)
    {
        var field = mock.CreateField("Ответственный мастер", fieldType: ProjectFieldType.UserLink);
        MockedProject.AssignFieldValues(mock.Character, new FieldWithValue(field, userId.Value.ToString()));
        return field;
    }
}
