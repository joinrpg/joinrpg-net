using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes.Plots;

namespace JoinRpg.DataModel;

public class PlotElement : IProjectEntity, IDeletableSubEntity
{
    public int PlotElementId { get; set; }

    public int ProjectId { get; set; }

    public virtual Project Project { get; set; }

    int IOrderableEntity.Id => PlotElementId;

    public int PlotFolderId { get; set; }

    public PlotFolder PlotFolder { get; set; }

    public virtual ICollection<CharacterGroup> TargetGroups { get; set; }

    public virtual ICollection<Character> TargetCharacters { get; set; }

    //TODO: Add here "mentioned characters" concept


    public virtual ICollection<PlotElementTexts> Texts { get; set; } = new HashSet<PlotElementTexts>();

    public DateTime CreatedDateTime { get; set; }

    public DateTime ModifiedDateTime { get; set; }

    public bool IsCompleted { get; set; }

    public bool CanBePermanentlyDeleted => false;
    public bool IsActive { get; set; }

    public PlotElementType ElementType { get; set; }

    public int? Published { get; set; }
}

//Sometimes we need to load bunch of plots w/o texts...
public class PlotElementTexts : IOrderableEntity
{
    public int PlotElementId { get; set; }


    public MarkdownString Content { get; set; } = new MarkdownString();

    public string TodoField { get; set; }

    public int Version { get; set; }
    public DateTime ModifiedDateTime { get; set; }
    public virtual User AuthorUser { get; set; }
    public int? AuthorUserId { get; set; }
    public PlotElement PlotElement { get; set; }

    int IOrderableEntity.Id => PlotElementId;
}
