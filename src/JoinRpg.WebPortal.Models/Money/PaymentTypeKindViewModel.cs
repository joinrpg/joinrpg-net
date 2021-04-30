using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
    /// <summary>
    /// Map of <see cref="PaymentTypeKind"/>
    /// </summary>
    public enum PaymentTypeKindViewModel
    {
        Custom = PaymentTypeKind.Custom,

        [Display(Name = "Наличными")]
        Cash = PaymentTypeKind.Cash,

        [Display(Name = "Онлайн")]
        Online = PaymentTypeKind.Online,
    }
}
