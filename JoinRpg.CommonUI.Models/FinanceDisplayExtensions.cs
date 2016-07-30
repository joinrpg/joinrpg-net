using System;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.CommonUI.Models
{
  public static class FinanceDisplayExtensions
  {
    public static string GetDisplayName([NotNull] this PaymentType paymentType)
    {
      if (paymentType == null) throw new ArgumentNullException(nameof(paymentType));
      return paymentType.IsCash ? paymentType.User.GetCashName() : paymentType.Name;
    }

    public static string GetCashName(this User user)
    {
      return "Наличными — " + user.DisplayName;
    }

  }
}
