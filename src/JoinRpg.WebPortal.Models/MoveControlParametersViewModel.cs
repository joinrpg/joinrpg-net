namespace JoinRpg.Web.Models;

[Obsolete]
public class MoveControlParametersViewModel
{
    public IMoveableNonInteractiveListItem Item { get; set; }
    public int ProjectId { get; set; }
    public int ListItemId { get; set; }
    public int ParentObjectId { get; set; }

    public string ActionName { get; set; } = "MoveElement";
    public string? ControllerName { get; set; }
}
