using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.Helpers;
using JoinRpg.Helpers.Validation;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Claims.Finance;

namespace JoinRpg.Web.Models;

public class FinOperationListItemViewModel : ILinkable
{
    [Display(Name = "# операции")]
    public int FinanceOperationId { get; }

    [Display(Name = "Внесено денег"), Required]
    public int Money { get; }

    [Display(Name = "Оплачено мастеру")]
    public UserInfoHeader? PaymentMaster { get; }

    [Display(Name = "Способ оплаты"), Required]
    public string PaymentTypeName { get; }

    [Display(Name = "Отметил"), Required]
    public UserInfoHeader MarkingMaster { get; }

    [Display(Name = "Дата внесения"), Required, DateShouldBeInPast]
    public DateTime OperationDate { get; }

    [Display(Name = "Заявка"), Required]
    public string Claim { get; }

    [Url, Display(Name = "Ссылка на заявку")]
    public string ClaimLink { get; }

    [Display(Name = "Игрок"), Required]
    public UserInfoHeader Player { get; }

    /// <summary>
    /// Полный профиль игрока с контактами — только для выгрузки; при показе страницы не загружается.
    /// </summary>
    [Display(Name = "Игрок")]
    public UserInfo? PlayerProfile { get; }

    LinkType ILinkable.LinkType => LinkType.Claim;

    string ILinkable.Identification => ClaimId.ToString();

    int? ILinkable.ProjectId => ProjectId;

    public int ProjectId { get; }

    public int ClaimId { get; }

    /// <summary>Сводка для диалога модерации, если текущий пользователь может подтвердить операцию прямо из списка.</summary>
    public FinanceModerationOperationView? FinanceModeration { get; }

    /// <summary>
    /// Строка списка операций. Из самой операции читаются только скаляры: заголовок заявки с игроком, автор
    /// операции и способ оплаты приходят заголовками, загруженными на весь список сразу (#5852).
    /// </summary>
    public FinOperationListItemViewModel(
        FinanceOperation fo,
        ClaimWithPlayer claim,
        UserInfoHeader markingMaster,
        UserInfo? playerProfile,
        IUriService uriService,
        ProjectInfo projectInfo,
        UserIdentification? currentUser)
    {
        var paymentType = fo.GetPaymentTypeId() is { } paymentTypeId
            ? projectInfo.ProjectFinanceSettings.GetRequiredPayment(paymentTypeId)
            : null;

        ProjectId = fo.ProjectId;
        PaymentMaster = paymentType?.User;
        PaymentTypeName =
            paymentType?.GetDisplayName()                                           // If this is a payment, get payment type
            ?? ((FinanceOperationTypeViewModel)fo.OperationType).GetDisplayName();  // if this is other operation type, just display operation type
        Claim = claim.CharacterName;
        Money = fo.MoneyAmount;
        OperationDate = fo.OperationDate;
        FinanceOperationId = fo.CommentId;
        MarkingMaster = markingMaster;
        Player = claim.Player;
        PlayerProfile = playerProfile;
        ClaimId = fo.ClaimId;
        ClaimLink = uriService.Get(this);
        // Онлайн-оплаты в списке «на модерации» есть, но модерации не требуют — у них кнопки не будет
        FinanceModeration = fo.ToModerationViewIfAllowed(projectInfo, currentUser);
    }
}
