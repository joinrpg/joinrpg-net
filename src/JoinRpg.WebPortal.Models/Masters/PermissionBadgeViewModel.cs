namespace JoinRpg.Web.Models.Masters;

using JoinRpg.PrimitiveTypes.Access;
using static JoinRpg.PrimitiveTypes.Access.Permission;

public record class PermissionBadgeViewModel(Permission Permission, bool Value)
{
    public bool Value { get; } = Value;
    public string Description { get; } = Descriptions[Permission];
    public string DisplayName { get; } = DisplayNames[Permission];
    public bool IsNone { get; } = Permission == None;

    public bool OnlyIfAccommodationEnabled = Permission == CanManageAccommodation || Permission == CanSetPlayersAccommodations;

    private readonly static Dictionary<Permission, string> DisplayNames = new()
    {
        {None, "" },
        {CanChangeFields, "Настраивать поля персонажа" },
        {CanChangeProjectProperties, "Настраивать проект" },
        {CanGrantRights, "Давать доступ другим мастерам" },
        {CanManageClaims, "Администратор заявок" },
        {CanEditRoles, "Редактировать ролевку" },
        {CanManageMoney, "Управлять финансами" },
        {CanSendMassMails, "Делать массовую рассылку" },
        {CanManagePlots, "Редактор сюжетов" },
        {CanManageAccommodation, "Настраивать поселение" },
        {CanSetPlayersAccommodations,  "Расселять игроков"}
    };

    private readonly static Dictionary<Permission, string> Descriptions = new()
    {
        {None, "" },
        {CanChangeFields, "может добавлять, удалять или редактировать поля заявки и поля персонажа" },
        {CanChangeProjectProperties, "может изменять свойства проекта, переименовывать его, отправлять проект в архив и т.д." },
        {CanGrantRights,  "может добавлять или удалять мастеров, настраивать права доступа" },
        {CanManageClaims, "может изменять статус заявок (принимать, отклонять, переносить в лист ожидания) и переназначать ответственного мастера для любой заявки в базе" },
        {CanEditRoles, "может добавлять новые группы или новых персонажей, редактировать и удалять группы и персонажей" },
        {CanManageMoney, "может настраивать размеры взносов и способы оплаты, отмечать взносы, принятые любым мастером, возвращать взносы и т.д." },
        {CanSendMassMails, "может разослать письма на емейл любой группе игроков (не только своим)" },
        {CanManagePlots,"может добавлять и удалять сюжеты и вводные, назначать группы и персонажей, которым они видны, публиковать вводные" },
        {CanManageAccommodation, "может добавлять/удалять номера и типы поселений" },
        {CanSetPlayersAccommodations, "может назначать игрокам номер"}
    };
}
