using System;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Helpers
{
  public static class DisplayAttributeHelper
  {
    public static string GetDisplayName(this Enum enumValue) => enumValue.GetAttribute<DisplayAttribute>().GetName();
  }
}
