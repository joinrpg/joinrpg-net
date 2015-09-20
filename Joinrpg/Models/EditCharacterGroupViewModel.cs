using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models
{

  public abstract class CharacterGroupViewModelBase : EditGameObjectViewModelBase
  {
    [DisplayName("Название локации"), Required]
    public string Name { get; set; }

    [DisplayName("Лимит прямых заявок"), Range(0, 100)]
    public int DirectSlots { get; set; }

    [DisplayName("Прямые заявки"),Description("Разрешены ли персонажи, кроме прописанных в сетке ролей АКА «И еще три стражника»")]
    public DirectClaimSettings HaveDirectSlots { get; set; }

    public bool HaveDirectSlotsForSave() => HaveDirectSlots != DirectClaimSettings.NoDirectClaims;

    public int DirectSlotsForSave() => HaveDirectSlots == DirectClaimSettings.DirectClaimsUnlimited ? -1 : DirectSlots;
  }

  public class EditCharacterGroupViewModel : CharacterGroupViewModelBase
  {
    public int CharacterGroupId { get; set; }

    public override IEnumerable<CharacterGroupListItemViewModel> PossibleParents => Data.PossibleParentsForGroup(CharacterGroupId);

    [ReadOnly(true)]
    public bool IsRoot { get; set; }

   public SubscribeSettingsViewModel Subscribe { get; set; } = new SubscribeSettingsViewModel();
  }

  public enum DirectClaimSettings
  {
    [Display(Name="Прямые заявки запрещены (только прописанные персонажи)")]
    NoDirectClaims,
    [Display(Name = "Прямые заявки разрешены")]
    DirectClaimsUnlimited,
    [Display(Name = "Прямые заявки разрешены, но не более лимита")]
    DirectClaimsLimited
  }

  public class AddCharacterGroupViewModel : CharacterGroupViewModelBase
  {
    public override IEnumerable<CharacterGroupListItemViewModel> PossibleParents => Data.ActiveGroups;
  }
}
