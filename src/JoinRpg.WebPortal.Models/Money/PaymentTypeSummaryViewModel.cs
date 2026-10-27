using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;

namespace JoinRpg.Web.Models;

public class PaymentTypeSummaryViewModel
{
    public PaymentTypeSummaryViewModel(PaymentTypeInfo pt, IEnumerable<FinanceOperation> projectFinanceOperations)
    {
        Name = pt.GetDisplayName();
        Master = pt.User;
        var operations = projectFinanceOperations
            .Where(fo => fo.GetPaymentTypeId() == pt.PaymentTypeId)
            .ToArray();

        Total = operations
            .Where(fo => fo.Approved)
            .Sum(fo => fo.MoneyAmount);

        Moderation = operations
            .Where(fo => fo.RequireModeration)
            .Sum(fo => fo.MoneyAmount);
    }

    [Display(Name = "Способ приема оплаты")]
    public string Name { get; }
    [Display(Name = "Мастер")]
    public UserInfoHeader Master { get; }
    [Display(Name = "Итого")]
    public int Total { get; }
    [Display(Name = "На модерации")]
    public int Moderation { get; }
}
