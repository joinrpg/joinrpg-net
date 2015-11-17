using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{

  public interface ICharacterGroupLinkViewModel
  {
    int CharacterGroupId { get; }
    string Name { get; }
    bool IsPublic { get; }
    int ProjectId { get; }
  }

  public class CharacterGroupLinkViewModel : ICharacterGroupLinkViewModel
  {
    public int CharacterGroupId { get; }
    public string Name { get; }
    public bool IsPublic { get; }
    public int ProjectId { get; }

    public CharacterGroupLinkViewModel(CharacterGroup group)
    {
      CharacterGroupId = group.CharacterGroupId;
      Name = group.CharacterGroupName;
      IsPublic = group.IsPublic;
      ProjectId = group.ProjectId;
    }
  }

  public class CharacterGroupListItemViewModel
  {
    public int RootGroupId
    { get; set; }
    public int CharacterGroupId { get; set; }

    [DisplayName("Название группы ролей")]
    public string Name { get; set; }

    public int DeepLevel { get; set; }

    public bool FirstCopy { get; set; }

    [DisplayName("Слотов для заявок в группу")]
    public int AvaiableDirectSlots { get; set; }

    public IList<CharacterViewModel> Characters { get; set; }

    public IEnumerable<CharacterViewModel> ActiveCharacters => Characters.Where(c => c.IsActive);

    public IEnumerable<CharacterViewModel> PublicCharacters => Characters.Where(c => c.IsActive && c.IsPublic);

    public HtmlString Description { get; set; }

    public IEnumerable<CharacterGroupListItemViewModel> Path { get; set; }

    public bool IsRoot => DeepLevel == 0;
    public bool IsActive { get; set; }
    public bool IsPublic { get; set; }

    public int ActiveClaimsCount { get; set; }

    public bool FirstInGroup { get; set; }
    public bool LastInGroup { get; set; }

    public int ProjectId { get; set; }
    public CharacterGroupListItemViewModel()
    {
      
    }
  }

}
