using System.Collections.Generic;
using System.ComponentModel;
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

    public HtmlString Description { get; set; }

    public IEnumerable<string> Path { get; set; }

    public bool IsRoot => DeepLevel == 0;
  }

}
