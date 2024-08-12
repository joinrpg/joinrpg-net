
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Interfaces.Email;

public sealed class RecepientData(string displayName, string email, IReadOnlyDictionary<string, string>? recipientSpecificValues = null)
{
    public string DisplayName { get; } = displayName ?? throw new ArgumentNullException(nameof(displayName));
    public string Email { get; } = email ?? throw new ArgumentNullException(nameof(email));
    public IReadOnlyDictionary<string, string> RecipientSpecificValues { get; } = recipientSpecificValues ?? new Dictionary<string, string>();

    public RecepientData(ProjectMasterInfo master) : this(master.Name.DisplayName, master.Email.Value)
    {

    }
}
