using System.ComponentModel;
using JoinRpg.DataModel;
using JoinRpg.Helpers.Validation;

namespace JoinRpg.Web.Models.CharacterGroups;

public class EditCharacterGroupViewModel : CharacterGroupViewModelBase, ICreatedUpdatedTracked
{
    public int CharacterGroupId { get; set; }

    [CannotBeEmpty, DisplayName("Является частью групп")]
    public List<string> ParentCharacterGroupIds { get; set; } = new List<string>();

    [ReadOnly(true)]
    public bool IsRoot { get; set; }

    public DateTime CreatedAt { get; set; }
    public User CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User UpdatedBy { get; set; }
}
