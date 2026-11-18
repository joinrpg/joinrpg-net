namespace JoinRpg.Web.ProjectMasterTools.Acl;

/// <summary>
/// Добавление мастера и правка его прав (ADR019, §4) — только с правом выдавать доступ.
/// Профиль уже добавленного мастера правится отдельно, через <see cref="IMasterProfileClient"/>.
/// </summary>
public interface IMasterAccessClient
{
    /// <summary>Форма добавления нового мастера — с ролью «Мастер». Бывшего возвращают правкой прав.</summary>
    Task<AddMasterViewModel> GetAddMaster(ProjectIdentification projectId, UserIdentification userId);

    Task AddMaster(AddMasterViewModel model);

    Task<MasterPermissionsViewModel> GetPermissions(ProjectIdentification projectId, UserIdentification userId);

    /// <summary>Сохранить права. Бывшего мастера это возвращает в проект с прежним профилем (ADR019, §1).</summary>
    Task SavePermissions(MasterPermissionsViewModel model);
}

public class MasterPermissionValueViewModel
{
    public required Permission Permission { get; set; }
    public bool Value { get; set; }
}

public class MasterPermissionsViewModel
{
    public required ProjectIdentification ProjectId { get; set; }
    public required UserIdentification UserId { get; set; }
    public required List<MasterPermissionValueViewModel> Permissions { get; set; }

    /// <summary>Мастер снят с проекта: сохранение прав вернёт его.</summary>
    public bool IsFormerMaster { get; set; }
}

/// <summary>Профиль и права нового мастера: поля профиля — те же, что при его правке.</summary>
public class AddMasterViewModel : MasterProfileViewModel
{
    /// <summary>Роль, которой предзаполнена форма добавления мастера.</summary>
    public const string DefaultRole = "Мастер";

    public required List<MasterPermissionValueViewModel> Permissions { get; set; }
}
