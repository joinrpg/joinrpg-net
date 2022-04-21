using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models;

public class PaymentTypeSummaryViewModel
{
    public PaymentTypeSummaryViewModel(PaymentType pt, ICollection<FinanceOperation> projectFinanceOperations)
    {
        Name = pt.GetDisplayName();
        Master = pt.User;
        Total = projectFinanceOperations
            .Where(fo => fo.PaymentTypeId == pt.PaymentTypeId && fo.Approved)
            .Sum(fo => fo.MoneyAmount);

        Moderation = projectFinanceOperations
            .Where(fo => fo.PaymentTypeId == pt.PaymentTypeId && fo.RequireModeration)
            .Sum(fo => fo.MoneyAmount);
    }

    [Display(Name = "Способ приема оплаты")]
    public string Name { get; }
    [Display(Name = "Мастер")]
    public User Master { get; }
    [Display(Name = "Итого")]
    public int Total { get; }
    [Display(Name = "На модерации")]
    public int Moderation { get; }
}
