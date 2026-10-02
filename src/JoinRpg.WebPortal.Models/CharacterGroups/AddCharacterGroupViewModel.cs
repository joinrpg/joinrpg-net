using JoinRpg.Helpers.Validation;

namespace JoinRpg.Web.Models.CharacterGroups;

public class AddCharacterGroupViewModel : CharacterGroupViewModelBase
{
    [CannotBeEmpty, DisplayName("Является частью групп")]
    public CharacterGroupIdentification[] ParentCharacterGroupIds { get; set; } = [];
}
