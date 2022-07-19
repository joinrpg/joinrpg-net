using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Web.Models.Characters;

public class AddCharacterViewModel : CharacterViewModelBase
{
    public AddCharacterViewModel Fill(CharacterGroup characterGroup, int currentUserId)
    {
        ProjectId = characterGroup.ProjectId;
        CharacterTypeInfo = CharacterTypeInfo.Default();
        FillFields(new Character()
        {
            Project = characterGroup.Project,
            ProjectId = ProjectId,
            IsAcceptingClaims = true,
            ParentCharacterGroupIds = new[] { characterGroup.CharacterGroupId },
        }, currentUserId);
        return this;
    }

    [Display(Name = "Добавить еще одного персонажа", Description = "После сохранения продолжить добавлять персонажей в эту группу")]
    public bool ContinueCreating { get; set; }
}
