using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.Comments;

public class CommentViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(CommentViewModel comment) => View("Comment", comment);
}
