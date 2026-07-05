using JoinRpg.Web.Games.FieldSetup;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.Fields;

public class FieldRowViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(GameFieldListItemViewModel field, IList<GameFieldListItemViewModel> allItems)
        => View("FieldRow", new FieldRowModel(field, allItems));
}

public record FieldRowModel(GameFieldListItemViewModel Field, IList<GameFieldListItemViewModel> AllItems);
