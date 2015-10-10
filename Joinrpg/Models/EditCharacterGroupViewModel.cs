using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{

  public abstract class CharacterGroupViewModelBase : EditGameObjectViewModelBase
  {
    [DisplayName("Название группы"), Required]
    public string Name { get; set; }

    [DisplayName("Лимит прямых заявок"), Range(0, 100)]
    public int DirectSlots { get; set; }

    [DisplayName("Прямые заявки"),Description("Разрешены ли персонажи, кроме прописанных в сетке ролей АКА «И еще три стражника»")]
    public DirectClaimSettings HaveDirectSlots { get; set; }


    [Display(Name = "Ответственный мастер для новых заявок")]
    public int ResponsibleMasterId { get; set; }

    [ReadOnly(true)]
    public IEnumerable<MasterListItemViewModel> Masters { get; set; }

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

  public class MasterListItemViewModel  
  {
    public string Id { get; set; }
    public string Name { get; set; }

    public static IEnumerable<MasterListItemViewModel> FromProject(Project project)
    {
      return project.ProjectAcls.Select(
        acl => new MasterListItemViewModel() {Id = acl.UserId.ToString(), Name = acl.User.DisplayName});
    }
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
