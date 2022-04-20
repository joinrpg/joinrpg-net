using JoinRpg.Helpers.Web;

namespace JoinRpg.Web.Models.Schedules
{
    public class TableHeaderViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public JoinHtmlString Description { get; set; }
    }
}
