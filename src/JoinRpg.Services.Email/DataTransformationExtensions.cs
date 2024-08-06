using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces.Email;

namespace JoinRpg.Services.Email;

internal static class DataTransformationExtensions
{
    public static RecepientData ToRecepientData(this User recipient, IReadOnlyDictionary<string, string>? values = null)
        => new(recipient.GetDisplayName(), recipient.Email, values);
}
