using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;

namespace JoinRpg.Web.Models;

public static class FinanceDisplayExtensions
{
    /// <summary>
    /// Returns display name of the payment type kind
    /// </summary>
    public static string GetDisplayName(this PaymentTypeKindViewModel kind, User? user, string? defaultName = null)
    {
        switch (kind)
        {
            case PaymentTypeKindViewModel.Custom:
                return defaultName ?? kind.GetDisplayName();
            case PaymentTypeKindViewModel.Cash:
                return user != null ? $@"{kind.GetDisplayName()} — {user.GetDisplayName()}" : kind.GetDisplayName();
            case PaymentTypeKindViewModel.Online:
                return kind.GetDisplayName();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// Returns display name of the payment type kind
    /// </summary>
    public static string GetDisplayName(this PaymentTypeKind kind, User user, string? defaultName = null)
        => ((PaymentTypeKindViewModel)kind).GetDisplayName(user, defaultName);

    /// <summary>
    /// Returns display name for the payment type
    /// </summary>
    public static string GetDisplayName([NotNull] this PaymentType paymentType)
    {
        if (paymentType == null)
        {
            throw new ArgumentNullException(nameof(paymentType));
        }

        return paymentType.TypeKind.GetDisplayName(paymentType.User, paymentType.Name);
    }

    /// <summary>
    /// Returns display name for the payment type view model
    /// </summary>
    public static string GetDisplayName([NotNull] this PaymentTypeViewModel paymentType)
    {
        if (paymentType == null)
        {
            throw new ArgumentNullException(nameof(paymentType));
        }

        return paymentType.TypeKind.GetDisplayName(paymentType.User, paymentType.Name);

    }

}
