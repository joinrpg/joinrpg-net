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
    public User? PaymentMaster { get; }

    [Display(Name = "Способ оплаты"), Required]
    public string PaymentTypeName { get; }

    [Display(Name = "Отметил"), Required]
    public User MarkingMaster { get; }

    [Display(Name = "Дата внесения"), Required, DateShouldBeInPast]
    public DateTime OperationDate { get; }

    [Display(Name = "Заявка"), Required]
    public string Claim { get; }

    [Url, Display(Name = "Ссылка на заявку")]
    public string ClaimLink { get; }

    [Display(Name = "Игрок"), Required]
    public User Player { get; }

    LinkType ILinkable.LinkType => LinkType.Claim;

    string ILinkable.Identification => ClaimId.ToString();

    int? ILinkable.ProjectId => ProjectId;

    public int ProjectId { get; }

    public int ClaimId { get; }

    /// <summary>Сводка для диалога модерации, если текущий пользователь может подтвердить операцию прямо из списка.</summary>
    public FinanceModerationOperationView? FinanceModeration { get; }

    public FinOperationListItemViewModel(FinanceOperation fo, IUriService uriService, ProjectInfo projectInfo, UserIdentification? currentUser)
    {
        ProjectId = fo.ProjectId;
        PaymentMaster = fo.PaymentType?.User;
        PaymentTypeName =
            fo.PaymentType?.GetDisplayName()                                        // If this is a payment, get payment type
            ?? ((FinanceOperationTypeViewModel)fo.OperationType).GetDisplayName();  // if this is other operation type, just display operation type
        Claim = fo.Claim.Character.CharacterName;
        Money = fo.MoneyAmount;
        OperationDate = fo.OperationDate;
        FinanceOperationId = fo.CommentId;
        MarkingMaster = fo.Comment.Author;
        Player = fo.Claim.Player;
        ClaimLink = uriService.Get(fo.Claim);
        ClaimId = fo.ClaimId;
        // Онлайн-оплаты в списке «на модерации» есть, но модерации не требуют — у них кнопки не будет
        FinanceModeration = fo.ToModerationViewIfAllowed(projectInfo, currentUser);
    }
}
