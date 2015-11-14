using System;
using System.Collections.Generic;
using System.Linq;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global — required by LINQ
  public class PlotFolder : IProjectEntity, IDeletableSubEntity
  {
    public int PlotFolderId { get; set; }
    public int ProjectId { get; set; }
    public virtual Project Project { get; set; }

    int IOrderableEntity.Id => PlotFolderId;

    //TODO: Decide if title should be for players or for masters or we need two titles
    public string MasterTitle { get; set; }

    public MarkdownString MasterSummary { get; set; } = new MarkdownString();

    public virtual ICollection<PlotElement> Elements { get; set; }

    public string TodoField { get; set; }

    public DateTime CreatedDateTime { get; set; }

    public DateTime ModifiedDateTime { get; set; }

    public virtual ICollection<CharacterGroup> RelatedGroups { get; set; }

    public bool IsActive { get; set; }

    public bool Completed => IsActive && string.IsNullOrWhiteSpace(TodoField) && Elements.All(e => e.IsCompleted) && Elements.Any();
    public bool InWork => IsActive && !Completed;
    public bool CanBePermanentlyDeleted => !Elements.Any();
  }
}