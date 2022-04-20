using JetBrains.Annotations;

namespace JoinRpg.Services.Interfaces.Email
{
    public sealed class RecepientData
    {
        [NotNull]
        public string DisplayName { get; }
        [NotNull]
        public string Email { get; }
        [NotNull]
        public IReadOnlyDictionary<string, string> RecipientSpecificValues { get; }

        public RecepientData([NotNull]
            string displayName,
            [NotNull]
            string email,
            [CanBeNull]
            IReadOnlyDictionary<string, string>? recipientSpecificValues = null)
        {
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Email = email ?? throw new ArgumentNullException(nameof(email));
            RecipientSpecificValues = recipientSpecificValues ?? new Dictionary<string, string>();
        }
    }
}
