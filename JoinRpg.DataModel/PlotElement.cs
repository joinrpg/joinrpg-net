using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global — required by LINQ
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

    [NotNull]
    public virtual ICollection<PlotElementTexts> Texts { get; set; } = new HashSet<PlotElementTexts>();

    public DateTime CreatedDateTime { get; set; }

    public DateTime ModifiedDateTime { get; set; }

    public bool IsCompleted { get; set; }

    public bool CanBePermanentlyDeleted => false;
    public bool IsActive { get; set; }

    public PlotElementType ElementType { get; set; }

    public int? Published { get; set; }
  }

  public enum PlotElementType
  {
    RegularPlot,
    Handout,
  }

  //Sometimes we need to load bunch of plots w/o texts...
  public class PlotElementTexts
  {
    public int PlotElementId { get; set; }

    [NotNull]
    public MarkdownString Content { get; set; } = new MarkdownString();

    public string TodoField { get; set; }

    public int Version { get; set; }
    public DateTime ModifiedDateTime { get; set; }
    public virtual User AuthorUser { get; set; }
    public int? AuthorUserId { get; set; }
    public PlotElement PlotElement { get; set; }
  }
}