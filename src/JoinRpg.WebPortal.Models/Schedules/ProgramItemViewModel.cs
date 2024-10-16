using JoinRpg.DataModel;
using JoinRpg.WebComponents;

namespace JoinRpg.Web.Models.Schedules;

public class ProgramItemViewModel
{
    public static ProgramItemViewModel Empty { get; } = new ProgramItemViewModel()
    {
        Id = -1,
        Name = "",
        Description = new MarkdownString(),
        ProjectId = -1,
        Users = [],
        IsEmpty = true,
    };
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required MarkdownString Description { get; set; }
    public required int ProjectId { get; set; }
    public required UserLinkViewModel[] Users { get; set; }

    public bool IsEmpty { get; private set; } = false;

    public int RowSpan { get; set; } = 1;
    public int ColSpan { get; set; } = 1;
}
