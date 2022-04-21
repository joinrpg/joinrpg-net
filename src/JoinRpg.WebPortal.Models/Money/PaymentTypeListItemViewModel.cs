using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
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
    public User Master { get; }

    public PaymentTypeListItemViewModel(PaymentType paymentType)
    {
        PaymentTypeId = paymentType.PaymentTypeId;
        ProjectId = paymentType.ProjectId;
        TypeKind = (PaymentTypeKindViewModel)paymentType.TypeKind;
        Master = paymentType.User;
        Name = TypeKind.GetDisplayName(null, paymentType.Name);
        IsActive = paymentType.IsActive;
        IsDefault = paymentType.IsDefault;
        CanBePermanentlyDeleted = IsActive
            && TypeKind == PaymentTypeKindViewModel.Custom
            && paymentType.CanBePermanentlyDeleted;
    }

    public PaymentTypeListItemViewModel(ProjectAcl acl)
    {
        PaymentTypeId = null;
        ProjectId = acl.ProjectId;
        Name = PaymentTypeKindViewModel.Cash.GetDisplayName();
        TypeKind = PaymentTypeKindViewModel.Cash;
        Master = acl.User;
        IsActive = false;
        IsDefault = false;
        CanBePermanentlyDeleted = false;
    }

    public PaymentTypeListItemViewModel(PaymentTypeKind typeKind, User user, int projectId)
    {
        Name = typeKind.GetDisplayName(user);
        PaymentTypeId = null;
        ProjectId = projectId;
        Master = user;
        TypeKind = (PaymentTypeKindViewModel)typeKind;
        CanBePermanentlyDeleted = false;
        IsDefault = false;
        IsActive = false;
    }
}
