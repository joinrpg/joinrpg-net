using System.Collections.Generic;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces.Email;

namespace JoinRpg.Services.Email
{
  internal static class DataTransformationExtensions
  {
      public static RecepientData ToRecepientData(this User recipient, IReadOnlyDictionary<string, string> values = null)
      {
          return new RecepientData(recipient.GetDisplayName(), recipient.Email, values);
      }
  }
}
