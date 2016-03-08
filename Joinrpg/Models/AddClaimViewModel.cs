using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models
{
  public class AddClaimViewModel
  {
    public int ProjectId { get; set; }

    public string ProjectName { get; set; }

    //TODO[MarkdownViewModel]
    public HtmlString ClaimApplyRules { get; set; }

    public int? CharacterId { get; set; }
    public int? CharacterGroupId { get; set; }

    [DisplayName("Заявка")]
    public string TargetName { get; set; }

    [Display(Name="Описание")]
    public MarkdownViewModel Description { get; set; }

    public bool HasApprovedClaim { get; set; }

    public bool HasAnyClaim { get; set; }

    public bool HasMyClaim { get; set; }

    public bool IsAvailable { get; set; }

    [Display(Name ="Комментарий к заявке", Description="Все, что вы хотите сообщить мастерам дополнительно")]
    public MarkdownViewModel ClaimText { get; set; }

    [ReadOnly(true)]
    public CustomFieldsViewModel Fields { get; private set; }

    public static AddClaimViewModel Create(Character character, User user)
    {
      var vm = new AddClaimViewModel().Fill(character, user);
      vm.CharacterId = character.CharacterId;
      return vm;
    }

    public static AddClaimViewModel Create(CharacterGroup group, User user)
    {
      var vm = new AddClaimViewModel().Fill(@group, user);
      vm.CharacterGroupId = group.CharacterGroupId;
      return vm;
    }

    public AddClaimViewModel Fill(IClaimSource obj, User user)
    {
      ProjectId = obj.ProjectId;
      ProjectName = obj.Project.ProjectName;
      HasAnyClaim = user.Claims.Any(c => c.ProjectId == obj.ProjectId && c.IsPending);
      HasApprovedClaim = !(obj.Project.Details?.EnableManyCharacters ?? false) && obj.Project.Claims.OfUserApproved(user.UserId).Any();
      HasMyClaim = obj.HasClaimForUser(user.UserId);
      TargetName = obj.Name;
      Description = new MarkdownViewModel(obj.Description);
      IsAvailable = obj.IsAvailable;
      ClaimApplyRules = obj.Project.Details?.ClaimApplyRules?.ToHtmlString();
      Fields = new CustomFieldsViewModel(user.UserId, obj.Project).OnlyClaimFields().EnableClaimAccess();
      IsRoot = obj.IsRoot;
      return this;
    }

    public bool IsRoot { get; private set; }
  }
}
