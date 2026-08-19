using JoinRpg.Web.Models.CharacterGroups;

namespace JoinRpg.Web.Models;

public class GameRolesViewModel
{

    public required string ProjectName { get; set; }

    public required bool ShowEditControls { get; set; }

    public required ProjectIdentification ProjectId { get; set; }

    /// <summary>
    /// Группа, явно указанная в URL, или null, если не указана — тогда сетка строится от корня
    /// проекта, не используя спецгруппы (передаётся острову ProjectRoleGrid как классическая сетка).
    /// </summary>
    public CharacterGroupIdentification? GridGroupId { get; set; }

    public required string RootGroupName { get; set; }

    /// <summary>
    /// Заголовок группы (breadcrumbs, описание). Null для страницы горячих ролей (GameGroups/Hot) —
    /// там сетка не привязана к одной группе, заголовок группы показывать не нужно.
    /// </summary>
    public CharacterGroupDetailsViewModel? Details { get; set; }
}
