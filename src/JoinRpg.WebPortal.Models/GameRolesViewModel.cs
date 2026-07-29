using JoinRpg.Web.Models.CharacterGroups;

namespace JoinRpg.Web.Models;

public class GameRolesViewModel
{

    public required string ProjectName { get; set; }

    public required bool ShowEditControls { get; set; }

    /// <summary>Корневая группа сетки — передаётся острову ProjectRoleGrid как классическая сетка.</summary>
    public required CharacterGroupIdentification RootGroupId { get; set; }

    public required string RootGroupName { get; set; }

    public required CharacterGroupDetailsViewModel Details { get; set; }
}
