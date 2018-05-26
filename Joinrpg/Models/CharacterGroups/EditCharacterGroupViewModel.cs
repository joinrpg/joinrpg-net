using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models.CharacterGroups
{

  public abstract class CharacterGroupViewModelBase : GameObjectViewModelBase
  {
    [DisplayName("Название группы"), Required]
    public string Name { get; set; }

    [Display(Name = "Лимит заявок", Description= "Не включает уже прописанных в сетке ролей"), Range(0, 100)]
    public int DirectSlots { get; set; }

    [Display(Name = "Заявки в группу", Description = "Разрешены ли персонажи, кроме прописанных в сетке ролей АКА «И еще три стражника»")]
    public DirectClaimSettings HaveDirectSlots { get; set; }


    [Display(
      Name = "Ответственный мастер для новых заявок", 
      Description = "Ответственный мастер, который будет назначен новым заявкам. Может быть переопределен в дочерних группах. Если ответственный мастер не установлен, он берется из родительской группы. Изменение этого поля не изменит существующие заявки.")]
    public int ResponsibleMasterId { get; set; }

    [ReadOnly(true)]
    public IEnumerable<MasterListItemViewModel> Masters { get; set; }

    public bool HaveDirectSlotsForSave() => HaveDirectSlots != DirectClaimSettings.NoDirectClaims;

    public int DirectSlotsForSave() => HaveDirectSlots == DirectClaimSettings.DirectClaimsUnlimited ? -1 : DirectSlots;
  }

  public class EditCharacterGroupViewModel : CharacterGroupViewModelBase, ICreatedUpdatedTracked
  {
    public int CharacterGroupId { get; set; }

    [ReadOnly(true)]
    public bool IsRoot { get; set; }

    public DateTime CreatedAt { get; set; }
    public User CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User UpdatedBy { get; set; }
  }

  public class MasterListItemViewModel  
  {
    public string Id { get; set; }
    public string Name { get; set; }
  }

  public static class MasterListExtensions
  {
    public static IEnumerable<MasterListItemViewModel> GetMasterListViewModel(this Project project)
    {
      return project.ProjectAcls.Select(
        acl => new MasterListItemViewModel() {Id = acl.UserId.ToString(), Name = acl.User.GetDisplayName() }).OrderBy(a => a.Name);
    }
  }

  public enum DirectClaimSettings
  {
    [Display(Name= "Заявки вне прописанных мастерами персонажей запрещены")]
    NoDirectClaims,
    [Display(Name = "Разрешены заявки в группу (без лимита)")]
    DirectClaimsUnlimited,
    [Display(Name = "Разрешены заявки в группу, но не более лимита")]
    DirectClaimsLimited,
  }

  public class AddCharacterGroupViewModel : CharacterGroupViewModelBase
  {
  }
}
