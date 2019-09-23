using System;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;

namespace JoinRpg.CommonUI.Models
{
    public static class FinanceDisplayExtensions
    {
        /// <summary>
        /// Returns display name of the payment type kind
        /// </summary>
        /// <param name="kind"></param>
        /// <param name="defaultName">Default name</param>
        /// <param name="user">User data</param>
        public static string GetDisplayName(this PaymentTypeKind kind, User user, string defaultName = null)
        {
            switch (kind)
            {
                case PaymentTypeKind.Custom:
                    return defaultName ?? kind.GetDisplayName();
                case PaymentTypeKind.Cash:
                    return user != null ? $@"{kind.GetDisplayName()} — {user.GetDisplayName()}" : kind.GetDisplayName();
                case PaymentTypeKind.Online:
                    return kind.GetDisplayName();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Returns display name for the payment type
        /// </summary>
        public static string GetDisplayName([NotNull] this PaymentType paymentType)
        {
            if (paymentType == null)
                throw new ArgumentNullException(nameof(paymentType));
            return paymentType.Kind.GetDisplayName(paymentType.User, paymentType.Name);
        }
    }
}
