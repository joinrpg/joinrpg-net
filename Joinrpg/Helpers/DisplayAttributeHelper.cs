using System;
using System.ComponentModel.DataAnnotations;
using JoinRpg.Helpers;

namespace JoinRpg.Web.Helpers
{
  public static class DisplayAttributeHelper
  {
    public static string GetDisplayName(this Enum enumValue) => enumValue.GetAttribute<DisplayAttribute>().GetName();
  }
}
