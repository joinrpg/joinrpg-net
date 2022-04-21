namespace JoinRpg.Web.Models.Print;

public class PrintIndexViewModel
{
    public PrintIndexViewModel(int projectId, IReadOnlyCollection<int> characterIds)
    {
        ProjectId = projectId;
        CharacterIds = characterIds;
    }

    public int ProjectId { get; }
    public IReadOnlyCollection<int> CharacterIds { get; }
}
