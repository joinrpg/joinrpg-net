using System;
using System.Collections.Generic;
using System.Linq;

namespace JoinRpg.DataModel
{
  public class PlotElement : IProjectSubEntity
  {
    public int PlotElementId { get; set; }

    public int ProjectId { get; set; }

    public Project Project { get; set; }

    int IProjectSubEntity.Id => PlotElementId;

    public int PlotFolderId { get; set; }

    public PlotFolder PlotFolder { get; set; }

    public MarkdownString MasterSummary { get; set; } 

    public virtual ICollection<CharacterGroup> TargetGroups { get; set; }

    public virtual ICollection<Character> TargetCharacters { get; set; }

    //TODO: Add here "mentioned characters" concept

    public MarkdownString Content { get; set; }

    public string TodoField { get; set; }

    public DateTime CreatedDateTime { get; set; }

    public DateTime ModifiedDateTime { get; set; }

    public bool IsCompleted { get; set; }

    public bool IsObsolete { get; set; }

    public IEnumerable<IWorldObject> Targets => TargetCharacters.Cast<IWorldObject>().Union(TargetGroups);
  }
}