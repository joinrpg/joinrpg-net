using System;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.CommonUI.Models
{
    public static class FinanceDisplayExtensions
    {
        public static string GetDisplayName([NotNull] this PaymentType paymentType)
        {
            if (paymentType == null) throw new ArgumentNullException(nameof(paymentType));
            switch (paymentType.TypeKind)
            {
                case PaymentTypeKind.Custom:
                    return paymentType.Name;
                case PaymentTypeKind.Cash:
                    return paymentType.User.GetCashName();
                default:
                    throw new ArgumentOutOfRangeException(nameof(paymentType), paymentType, "Incorrect value");
            }
        }

        public static string GetCashName(this User user) => "Наличными — " + user.GetDisplayName();
    }
}
