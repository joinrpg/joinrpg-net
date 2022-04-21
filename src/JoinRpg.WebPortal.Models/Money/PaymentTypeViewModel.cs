using System.ComponentModel;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models;

public class PaymentTypeViewModel : PaymentTypeViewModelBase
{
    public int PaymentTypeId { get; set; }
    public bool IsDefault { get; set; }
    public PaymentTypeKindViewModel TypeKind { get; set; }
    public int UserId { get; set; }

    [ReadOnly(true)]
    public User User { get; }

    public PaymentTypeViewModel() { }

    public PaymentTypeViewModel(PaymentType source)
    {
        PaymentTypeId = source.PaymentTypeId;
        IsDefault = source.IsDefault;
        TypeKind = (PaymentTypeKindViewModel)source.TypeKind;
        Name = source.GetDisplayName();
        ProjectId = source.ProjectId;
        UserId = source.UserId;
        User = source.User;
    }
}
