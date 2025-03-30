using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
    /// Данные как они пришли с когдаигры. После привязки перекладываем в новые поля таблицы ProjectDetails
    /// </summary>
    public string JsonGameData { get; set; }

    public string Name { get; set; }

    /// <summary>
    /// Привязанные проекты. Пусто = ничего не привязано.
    /// </summary>

    public virtual HashSet<Project> Projects { get; set; } = [];

    public override string ToString() => $"KogdaIgraGame(Id={KogdaIgraGameId}, UpdateRequestedAt={UpdateRequestedAt}, LastUpdatedAt={LastUpdatedAt}, Name={Name}, JsonGameData=({JsonGameData}, Projects={Projects.Select(p => p.ProjectId.ToString()).JoinStrings(",")})";

}
