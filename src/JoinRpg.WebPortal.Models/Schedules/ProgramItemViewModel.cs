using JoinRpg.DataModel;

namespace JoinRpg.Web.Models.Schedules
{
    public class ProgramItemViewModel
    {
        public static ProgramItemViewModel Empty { get; } = new ProgramItemViewModel()
        {
            Id = -1,
            Name = "",
            Description = new MarkdownString(),
            ProjectId = -1,
            Users = Array.Empty<User>(),
            IsEmpty = true,
        };
        public int Id { get; set; }
        public string Name { get; set; }
        public MarkdownString Description { get; set; }
        public int ProjectId { get; set; }
        public User[] Users { get; set; }
        public bool IsEmpty { get; private set; } = false;

        public int RowSpan { get; set; } = 1;
        public int ColSpan { get; set; } = 1;
    }
}
