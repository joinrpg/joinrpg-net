using JoinRpg.Helpers.Web;

namespace JoinRpg.Web.Models.Schedules;

public class TableHeaderViewModel
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required JoinHtmlString Description { get; set; }
}
