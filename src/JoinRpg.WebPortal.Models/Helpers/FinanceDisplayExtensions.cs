using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Helpers;

namespace JoinRpg.Web.Models;

public static class FinanceDisplayExtensions
{
    /// <summary>
    /// Returns display name of the payment type kind
    /// </summary>
    public static string GetDisplayName(this PaymentTypeKindViewModel kind, User? user, string? defaultName = null)
        => kind.GetDisplayName(user?.ToUserInfoHeader(), defaultName);

    /// <summary>
    /// Returns display name of the payment type kind
    /// </summary>
    public static string GetDisplayName(this PaymentTypeKindViewModel kind, UserInfoHeader? user, string? defaultName = null)
    {
        return kind switch
        {
            PaymentTypeKindViewModel.Custom => defaultName ?? kind.GetDisplayName(),
            PaymentTypeKindViewModel.Cash => user != null ? $@"{kind.GetDisplayName()} — {user.DisplayName.DisplayName}" : kind.GetDisplayName(),
            PaymentTypeKindViewModel.Online => kind.GetDisplayName(),
            PaymentTypeKindViewModel.OnlineSubscription => kind.GetDisplayName(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    /// <summary>
    /// Returns display name of the payment type kind
    /// </summary>
    public static string GetDisplayName(this PaymentTypeKind kind, User user, string? defaultName = null)
        => ((PaymentTypeKindViewModel)kind).GetDisplayName(user, defaultName);

    /// <summary>
    /// Returns display name of the payment type kind
    /// </summary>
    public static string GetDisplayName(this PaymentTypeKind kind, UserInfoHeader user, string? defaultName = null)
        => ((PaymentTypeKindViewModel)kind).GetDisplayName(user, defaultName);

    /// <summary>
    /// Returns display name for the payment type
    /// </summary>
    public static string GetDisplayName(this PaymentType paymentType)
    {
        ArgumentNullException.ThrowIfNull(paymentType);

        return paymentType.TypeKind.GetDisplayName(paymentType.User, paymentType.Name);
    }

}
