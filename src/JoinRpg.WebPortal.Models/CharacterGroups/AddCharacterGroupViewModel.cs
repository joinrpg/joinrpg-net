using System.ComponentModel;
using JoinRpg.Helpers.Validation;

namespace JoinRpg.Web.Models.CharacterGroups;

public class AddCharacterGroupViewModel : CharacterGroupViewModelBase
{
    [CannotBeEmpty, DisplayName("Является частью групп")]
    public int[] ParentCharacterGroupIdInts { get; set; } = [];
}
