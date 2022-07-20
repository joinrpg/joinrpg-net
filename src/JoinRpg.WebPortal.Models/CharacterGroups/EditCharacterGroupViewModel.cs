using System.ComponentModel;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models.CharacterGroups;

public class EditCharacterGroupViewModel : CharacterGroupViewModelBase, ICreatedUpdatedTracked
{
    public int CharacterGroupId { get; set; }

    [ReadOnly(true)]
    public bool IsRoot { get; set; }

    public DateTime CreatedAt { get; set; }
    public User CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User UpdatedBy { get; set; }

    public bool ShowConvertToSlotButton { get; set; }
}
