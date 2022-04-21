using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers.Validation;

namespace JoinRpg.Web.Models;

public class PaymentTransferViewModel : AddCommentViewModel
{
    [Display(Name = "Дата внесения"), Required, DateShouldBeInPast]
    public DateTime OperationDate { get; set; }

    [Required]
    public int ClaimId { get; set; }

    [Display(Name = "Исходная заявка")]
    public string ClaimName { get; }

    /// <summary>
    /// Id of a claim to transfer money to
    /// </summary>
    [Required]
    [Display(Name = "Заявка для перевода взноса")]
    public int RecipientClaimId { get; set; }

    /// <summary>
    /// Money to transfer
    /// </summary>
    [Required(ErrorMessage = "Необходимо указать сумму перевода")]
    [Range(1, 50000, ErrorMessage = "Сумма перевода должна быть от 1 до 50000")]
    [Display(Name = "Перевести средства")]
    public int Money { get; set; }

    /// <summary>
    /// Max value allowed to transfer from this claim
    /// </summary>
    [Display(Name = "Доступно средств")]
    public int MaxMoney { get; }

    /// <summary>
    /// Comment text
    /// </summary>
    [Required(ErrorMessage = "Заполните текст комментария")]
    [DisplayName("Причина перевода")]
    [Description("Опишите вкратце причину перевода — например, оплата была сделана за несколько людей, или перезачет взноса")]
    [UIHint("MarkdownString")]
    public new string CommentText
    {
        get => base.CommentText;
        set => base.CommentText = value;
    }

    /// <summary>
    /// List of claims to select recipient claim from
    /// </summary>
    public IReadOnlyCollection<RecipientClaimViewModel> Claims { get; set; }

    public PaymentTransferViewModel() { }

    public PaymentTransferViewModel(Claim claim, IEnumerable<Claim> claims) : base()
    {
        OperationDate = DateTime.UtcNow;
        ActionName = "Перевести";
        ClaimId = claim.ClaimId;
        ClaimName = claim.Name;
        ProjectId = claim.ProjectId;
        CommentDiscussionId = claim.CommentDiscussionId;
        MaxMoney = claim.GetPaymentSum();
        Claims = claims
            .Where(c => c.ClaimId != ClaimId)
            .Select(c => new RecipientClaimViewModel(c))
            .OrderBy(c => c.Text)
            .ToArray();
    }
}
