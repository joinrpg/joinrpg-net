namespace JoinRpg.Data.Interfaces;

public class ProjectWithUpdateDateDto
{
    public int ProjectId { get; set; }
    public required string ProjectName { get; set; }
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Дата игры по наиболее поздней карточке КогдаИгры. Null — привязки к КогдаИгре нет.
    /// </summary>
    public DateTime? KogdaIgraGameDate { get; set; }
}
