using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel.Projects;

public class KogdaIgraGame
{
    /// <summary>
    /// Ид КогдаИгры
    /// </summary>
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int KogdaIgraGameId { get; set; }

    /// <summary>
    /// Выставляется датой апдейта с когда игры, когда игра оказалась в списке обновленных игр.
    /// Если UpdateRequestedAt > LastUpdatedAt данные нужно обновить
    /// </summary>
    public DateTimeOffset UpdateRequestedAt { get; set; }

    /// <summary>
    /// Выставляется датой апдейта, когда данные были загружены с КогдаИгры.
    /// </summary>
    public DateTimeOffset? LastUpdatedAt { get; set; }

    /// <summary>
    /// Данные как они пришли с когдаигры. 
    /// </summary>
    public string JsonGameData { get; set; }

    public string Name { get; set; }

    public bool Active { get; set; } = true;

    public string RegionName { get; set; } = "";

    /// <summary>
    /// Субрегион проведения игры (sub_region_id из КогдаИгры).
    /// NULL — регион неизвестен: данных не было или API не вернула.
    /// Id вне справочника <see cref="Region"/> может храниться как есть до ближайшей синхронизации игры.
    /// </summary>
    public Region? RegionId { get; set; }

    public DateTime? Begin { get; set; }
    public DateTime? End { get; set; }
    public string MasterGroupName { get; set; }
    public string? SiteUri { get; set; }
    public string? VkClub { get; set; }
    public string? LjComm { get; set; }
    public string? TelegramChannel { get; set; }

    /// <summary>
    /// Числовой код статуса игры на КогдаИгре (0=OK, 1=Прошла, 3=Отложена, 5=Отменена и т.д.).
    /// NULL — статус неизвестен: данных ещё не было (запись-заглушка) или игра удалена и JSON затёрт.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// Привязанные проекты. Пусто = ничего не привязано.
    /// </summary>

    public virtual HashSet<Project> Projects { get; set; } = [];

    public override string ToString() => $"KogdaIgraGame(Id={KogdaIgraGameId}, UpdateRequestedAt={UpdateRequestedAt}, LastUpdatedAt={LastUpdatedAt}, Name={Name}, JsonGameData=({JsonGameData}, Projects={Projects.Select(p => p.ProjectId.ToString()).JoinStrings(",")})";

}
