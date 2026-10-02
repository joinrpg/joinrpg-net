using JoinRpg.Web.Models.CharacterGroups;

namespace JoinRpg.Web.Models;

public class GameRolesViewModel
{

    public required string ProjectName { get; set; }

    /// <summary>
    /// Нужны ли кнопки правки в обрамлении группы. Только для классической сетки: на сетке по
    /// умолчанию обрамления нет, а свои кнопки остров показывает по собственным правам.
    /// </summary>
    public bool ShowEditControls { get; set; }

    public required ProjectIdentification ProjectId { get; set; }

    /// <summary>
    /// Группа, явно указанная в URL, или null, если не указана — тогда сетка строится от корня
    /// проекта, не используя спецгруппы (передаётся острову ProjectRoleGrid как классическая сетка).
    /// </summary>
    public CharacterGroupIdentification? GridGroupId { get; set; }

    /// <summary>
    /// Сохранённая сетка ролей, которую надо показать (сетка по умолчанию на /{projectId}/roles).
    /// Null — показываем классическую (транзиентную) сетку по <see cref="GridGroupId"/>.
    /// </summary>
    public ProjectRolesListIdentification? RolesListId { get; set; }

    /// <summary>
    /// Имя для заголовка страницы классической сетки. На сетке по умолчанию не используется:
    /// заголовок там такой же, как на /{projectId}/roleslist/{id}.
    /// </summary>
    public string? RootGroupName { get; set; }

    /// <summary>
    /// Заголовок группы (breadcrumbs, описание). Null для страницы горячих ролей (GameGroups/Hot) —
    /// там сетка не привязана к одной группе, заголовок группы показывать не нужно.
    /// </summary>
    public CharacterGroupDetailsViewModel? Details { get; set; }
}
