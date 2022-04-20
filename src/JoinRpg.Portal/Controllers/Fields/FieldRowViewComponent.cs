using JoinRpg.Web.Models.FieldSetup;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.Fields
{
    public class FieldRowViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(GameFieldEditViewModel field) => View("FieldRow", field);
    }
}
