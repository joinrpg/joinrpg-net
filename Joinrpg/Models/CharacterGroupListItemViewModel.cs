using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace JoinRpg.Web.Models
{
  public class CharacterGroupListItemViewModel
  {
    public int CharacterGroupId { get; set; }

    [DisplayName("Название локации (группы)")]
    public string Name { get; set; }

    public int DeepLevel { get; set; }

    public bool FirstCopy { get; set; }

    [DisplayName("Слотов в локации")]
    public int AvaiableDirectSlots { get; set; }

    public IList<CharacterViewModel> Characters { get; set; }

    public IEnumerable<CharacterViewModel> PublicCharacters => Characters.Where(c => c.IsPublic);

    public HtmlString Description { get; set; }

    public IEnumerable<CharacterGroupListItemViewModel> Path { get; set; }

    public bool IsRoot => DeepLevel == 0;
    public bool IsActive { get; set; }
    public bool IsPublic { get; set; }
  }

}
