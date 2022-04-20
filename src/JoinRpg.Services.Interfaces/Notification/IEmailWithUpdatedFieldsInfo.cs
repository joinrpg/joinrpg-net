using JoinRpg.DataModel;
using JoinRpg.Domain.CharacterFields;

namespace JoinRpg.Services.Interfaces.Notification
{
    /// <summary>
    /// This interface to be implemented by emails that should include the list of updated felds in them.
    /// </summary>
    public interface IEmailWithUpdatedFieldsInfo
    {
        /// <summary>
        /// Project fields that changed
        /// </summary>
        IReadOnlyCollection<FieldWithPreviousAndNewValue> UpdatedFields { get; }
        /// <summary>
        /// Entity the updated fields belong to
        /// </summary>
        IFieldContainter FieldsContainer { get; }
        /// <summary>
        /// Other attributes that have changed. Those attributes don't need access verification
        /// </summary>
        IReadOnlyDictionary<string, PreviousAndNewValue> OtherChangedAttributes { get; }
    }
}
