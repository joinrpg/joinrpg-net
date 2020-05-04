using System.Drawing;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models
{
    public class MoveControlParametersViewModel
    {
        public IMovableListItem Item { get; set; }
        public int ProjectId { get; set; }
        public int ListItemId { get; set; }
        public int ParentObjectId { get; set; }

        public string ActionName { get; set; } = "MoveElement";
        public string ControllerName { get; set; }
    }
}
