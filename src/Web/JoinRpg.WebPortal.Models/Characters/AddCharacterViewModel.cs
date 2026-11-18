using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Web.Models.Characters;

public class AddCharacterViewModel : CharacterViewModelBase
{
    public AddCharacterViewModel Fill(CharacterGroup characterGroup, int currentUserId, ProjectInfo projectInfo)
    {
        ProjectId = characterGroup.ProjectId;
        CharacterTypeInfo = CharacterTypeInfo.Default();
        // Персонаж только создаётся, значений полей у него нет — значит, нет и ссылок
        // на пользователей, грузить некого.
        FillFields(new Character()
        {
            Project = characterGroup.Project,
            ProjectId = ProjectId,
            IsAcceptingClaims = true,
            ParentCharacterGroupIds = new[] { characterGroup.CharacterGroupId },
        }, currentUserId, projectInfo, FieldUserLinksLoader.None);
        return this;
    }

    [Display(Name = "Добавить еще одного персонажа", Description = "После сохранения продолжить добавлять персонажей в эту группу")]
    public bool ContinueCreating { get; set; }
}
