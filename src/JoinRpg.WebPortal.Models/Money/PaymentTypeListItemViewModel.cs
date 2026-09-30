using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Helpers;

namespace JoinRpg.Web.Models;

public class PaymentTypeListItemViewModel
{
    public int? PaymentTypeId { get; }
    public int ProjectId { get; }

    [Display(Name = "Название")]
    public string Name { get; }

    public PaymentTypeKindViewModel TypeKind { get; }

    public bool IsActive { get; }

    [Display(Name = "Основной")]
    public bool IsDefault { get; }

    public bool CanBePermanentlyDeleted { get; }

    [Display(Name = "Ответственный")]
    public UserInfoHeader Master { get; }

    public PaymentTypeListItemViewModel(PaymentTypeInfo paymentType)
    {
        PaymentTypeId = paymentType.PaymentTypeId.PaymentTypeId;
        ProjectId = paymentType.PaymentTypeId.ProjectId.Value;
        TypeKind = (PaymentTypeKindViewModel)paymentType.TypeKind;
        Master = paymentType.User;
        Name = TypeKind.GetDisplayName((UserInfoHeader?)null, paymentType.Name);
        IsActive = paymentType.Enabled;
        IsDefault = paymentType.IsDefault;
        // PaymentType.CanBePermanentlyDeleted всегда false: типы оплаты только выключаются (soft-delete).
        CanBePermanentlyDeleted = false;
    }

    /// <summary>
    /// Потенциальные наличные: мастер проекта, у которого своего типа оплаты «наличные» ещё нет.
    /// </summary>
    public PaymentTypeListItemViewModel(ProjectMasterInfo master, ProjectIdentification projectId)
    {
        PaymentTypeId = null;
        ProjectId = projectId.Value;
        Name = PaymentTypeKindViewModel.Cash.GetDisplayName();
        TypeKind = PaymentTypeKindViewModel.Cash;
        Master = master.UserInfo;
        IsActive = false;
        IsDefault = false;
        CanBePermanentlyDeleted = false;
    }

    public PaymentTypeListItemViewModel(PaymentTypeKind typeKind, UserInfoHeader user, ProjectIdentification projectId)
    {
        Name = typeKind.GetDisplayName(user);
        PaymentTypeId = null;
        ProjectId = projectId.Value;
        Master = user;
        TypeKind = (PaymentTypeKindViewModel)typeKind;
        CanBePermanentlyDeleted = false;
        IsDefault = false;
        IsActive = false;
    }
}
