using JoinRpg.DomainTypes.ProjectMetadata.Payments;

namespace JoinRpg.Web.Models;

public class PaymentTypeViewModel : PaymentTypeViewModelBase
{
    public int PaymentTypeId { get; set; }
    public bool IsDefault { get; set; }
    public PaymentTypeKindViewModel TypeKind { get; set; }
    public int UserId { get; set; }

    public PaymentTypeViewModel() { }

    public PaymentTypeViewModel(PaymentTypeInfo source)
    {
        PaymentTypeId = source.PaymentTypeId.PaymentTypeId;
        IsDefault = source.IsDefault;
        TypeKind = (PaymentTypeKindViewModel)source.TypeKind;
        Name = source.TypeKind.GetDisplayName(source.User, source.Name);
        ProjectId = source.PaymentTypeId.ProjectId;
        UserId = source.User.UserId.Value;
    }
}
