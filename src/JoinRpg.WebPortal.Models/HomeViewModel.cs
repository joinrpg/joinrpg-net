using System.Collections.Generic;

namespace JoinRpg.Web.Models
{
    public class HomeViewModel
    {
        public IEnumerable<ProjectListItemViewModel> ActiveProjects { get; set; } = new List<ProjectListItemViewModel>();
        public bool HasMoreProjects { get; set; }
    }
}
