using System.ComponentModel;
using JoinRpg.Helpers.Validation;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Web.Models.CharacterGroups;

public class EditCharacterGroupViewModel : CharacterGroupViewModelBase
{
    public int CharacterGroupId { get; set; }

    [CannotBeEmpty, DisplayName("Является частью групп")]
    public int[] ParentCharacterGroupIdInts { get; set; } = [];

    public CreateUpdateMarksViewModel? Marks { get; set; }
}
