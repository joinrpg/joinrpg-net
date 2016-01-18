using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using JoinRpg.DataModel;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models
{
  public class AddClaimViewModel
  {
    public int ProjectId { get; set; }

    public string ProjectName { get; set; }

    public HtmlString ClaimApplyRules { get; set; }

    public int? CharacterId { get; set; }
    public int? CharacterGroupId { get; set; }

    [DisplayName("Заявка")]
    public string TargetName { get; set; }

    [Display(Name="Описание")]
    public MarkdownViewModel Description { get; set; }

    public bool HasApprovedClaim { get; set; }

    public bool HasAnyClaim { get; set; }

    public bool IsAvailable { get; set; }

    [Display(Name ="Комментарий к заявке", Description="Все, что вы хотите сообщить мастерам дополнительно"), Required]
    public MarkdownViewModel ClaimText { get; set; }

    [ReadOnly(true)]
    public CustomFieldsViewModel Fields { get; private set; }

    public static AddClaimViewModel Create(Character character, User user)
    {
      var vm = CreateImpl(character, user);
      vm.CharacterId = character.CharacterId;
      return vm;
    }

    public static AddClaimViewModel Create(CharacterGroup group, User user)
    {
      var vm = CreateImpl(@group, user);
      vm.CharacterGroupId = group.CharacterGroupId;
      return vm;
    }

    private static AddClaimViewModel CreateImpl(IClaimSource obj, User user)
    {
      var addClaimViewModel = new AddClaimViewModel
      {
        ProjectId = obj.ProjectId,
        ProjectName = obj.Project.ProjectName,
        HasAnyClaim = user.Claims.Any(c => c.ProjectId == obj.ProjectId && c.IsActive),
        HasApprovedClaim = user.Claims.Any(c => c.ProjectId == obj.ProjectId && c.IsApproved),
        TargetName = obj.Name,
        Description = new MarkdownViewModel(obj.Description),
        IsAvailable = obj.IsAvailable,
        ClaimApplyRules = obj.Project.Details?.ClaimApplyRules?.ToHtmlString(),
        Fields = new CustomFieldsViewModel(user.UserId, obj.Project).OnlyClaimFields().EnableClaimAccess(),
        IsRoot = obj.IsRoot
      };
      return addClaimViewModel;
    }

    public bool IsRoot { get; private set; }
  }
}
