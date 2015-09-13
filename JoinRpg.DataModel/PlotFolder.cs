using System;
using System.Collections.Generic;
using System.Linq;

namespace JoinRpg.DataModel
{
  public class PlotFolder : IProjectSubEntity
  {
    public int PlotFolderId { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; }

    int IProjectSubEntity.Id => PlotFolderId;

    //TODO: Decide if title should be for players or for masters or we need two titles
    public string MasterTitle { get; set; }

    public MarkdownString MasterSummary { get; set; } = new MarkdownString();

    public virtual ICollection<PlotElement> Elements { get; set; }

    public string TodoField { get; set; }

    public DateTime CreatedDateTime { get; set; }

    public DateTime ModifiedDateTime { get; set; }

    public virtual ICollection<CharacterGroup> RelatedGroups { get; set; }

    public bool IsObsolete { get; set; }

    public bool Completed()
    {
      return string.IsNullOrWhiteSpace(TodoField) && Elements.All(e => e.IsCompleted) && Elements.Any();
    }
  }
}