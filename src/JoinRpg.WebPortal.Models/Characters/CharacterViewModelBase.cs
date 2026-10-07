using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DataModel;
using JoinRpg.Domain.Access;
using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Web.Models.Characters;

public abstract class CharacterViewModelBase : IProjectIdAware
{
    /// <summary>
    /// Сообщение об ошибке, когда проект требует группы, а мастер не выбрал ни одной.
    /// </summary>
    /// <remarks>
    /// Проверка живёт в <c>CharacterController</c>, а не в проверке модели через
    /// <c>IValidatableObject</c>: <see cref="AllowToSetGroups"/> сюда попадает только при
    /// отрисовке формы (Fill на GET), при POST свойство остаётся <c>false</c> — модельная
    /// проверка не срабатывала, пустой список доезжал до сервиса и падал там сырой
    /// JoinValidationException (#5323).
    /// </remarks>
    public const string GroupsRequiredErrorMessage = "Персонаж должен принадлежать хотя бы к одной группе";

    public int ProjectId { get; set; }

    [ReadOnly(true)]
    public string ProjectName { get; set; }

    [Required]
    public CharacterTypeInfo CharacterTypeInfo { get; set; }

    [ReadOnly(true)]
    public bool CharactersHaveNameField { get; set; }

    [DisplayName("Имя персонажа")]
    public string Name { get; set; }

    public CustomFieldsViewModel Fields { get; set; }

    [DisplayName("Является частью групп")]
    public CharacterGroupIdentification[] ParentCharacterGroupIds { get; set; } = [];

    [ReadOnly(true)]
    public bool AllowToSetGroups { get; set; }

    protected void FillFields(
        Character field,
        int currentUserId,
        ProjectInfo projectInfo,
        IReadOnlyDictionary<UserIdentification, UserInfoHeader> fieldUsers)
    {
        Fields = new CustomFieldsViewModel(field, projectInfo, AccessArgumentsFactory.Create(field, new UserIdentification(currentUserId), projectInfo), fieldUsers);
        CharactersHaveNameField = projectInfo.CharacterNameField is not null;
        AllowToSetGroups = projectInfo.GroupTree.AllowToSetGroups;
    }
}
