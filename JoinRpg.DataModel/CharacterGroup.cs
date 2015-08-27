using System.Collections.Generic;

namespace JoinRpg.DataModel
{
  public class CharacterGroup : IClaimSource
  {

    public int CharacterGroupId { get; set; }

    public int ProjectId { get; set; }
    int IProjectSubEntity.Id => CharacterGroupId;


    public virtual Project Project { get; set; }

    public string CharacterGroupName { get; set; }

    public bool IsRoot { get; set; }

    public virtual ICollection<CharacterGroup> ParentGroups { get; set; }
    string IWorldObject.Name => CharacterGroupName;

    public virtual ICollection<CharacterGroup> ChildGroups { get; set; }

    public bool IsPublic { get; set; }

    public int AvaiableDirectSlots { get; set; }

    public virtual ICollection<Character> Characters { get; set; }

    public bool IsActive { get; set; }

    public MarkdownString Description { get; set; } = new MarkdownString();

    public bool IsAvailable => AvaiableDirectSlots > 0;

    public virtual ICollection<Claim>  Claims { get; set; }

    public virtual ICollection<PlotFolder> DirectlyRelatedPlotFolders { get; set; }

    public virtual ICollection<PlotElement>  DirectlyRelatedPlotElements { get; set; }
  }

}
