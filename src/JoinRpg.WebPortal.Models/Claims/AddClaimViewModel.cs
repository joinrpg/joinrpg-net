using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers.Web;
using JoinRpg.Markdown;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Web.Models;

public class AddClaimViewModel : IProjectIdAware
{
    public int ProjectId { get; set; }

    public string ProjectName { get; set; }

    public JoinHtmlString ClaimApplyRules { get; set; }

    public int? CharacterId { get; set; }
    public int? CharacterGroupId { get; set; }

    [DisplayName("Заявка")]
    public string TargetName { get; set; }

    [Display(Name = "Описание")]
    public JoinHtmlString Description { get; set; }

    public IReadOnlyCollection<AddClaimForbideReason> ValidationStatus
    {
        get;
        private set;
    }

    [Display(Name = "Комментарий к заявке",
         Description = "Все, что вы хотите сообщить мастерам дополнительно"),
     UIHint("MarkdownString")]
    public string ClaimText { get; set; }

    [ReadOnly(true)]
    public CustomFieldsViewModel Fields { get; private set; }

    public bool WarnForAnotherClaim { get; private set; }

    public static AddClaimViewModel Create(Character character, int playerUserId, ProjectInfo projectInfo)
        => new AddClaimViewModel { CharacterId = character.CharacterId }.Fill(character, playerUserId, projectInfo);

    public static AddClaimViewModel Create(CharacterGroup group, int playerUserId, ProjectInfo projectInfo)
        => new AddClaimViewModel { CharacterGroupId = group.CharacterGroupId }.Fill(group, playerUserId, projectInfo);

    public AddClaimViewModel Fill(IClaimSource claimSource, int playerUserId, ProjectInfo projectInfo, Dictionary<int, string?>? overrideValues = null)
    {
        var disallowReasons = claimSource.ValidateIfCanAddClaim(playerUserId).ToList();

        IsProjectRelatedReason = disallowReasons.Intersect(
            [
                AddClaimForbideReason.ProjectClaimsClosed,
                AddClaimForbideReason.ProjectNotActive,
            ])
            .Any();



        WarnForAnotherClaim = claimSource.Project.Claims.OfUserActive(playerUserId).Any();

        ValidationStatus = disallowReasons;
        ProjectAllowsMultipleCharacters = claimSource.Project.Details.EnableManyCharacters;

        ProjectId = claimSource.Project.ProjectId;
        ProjectName = claimSource.Project.ProjectName;
        TargetName = claimSource.Name;
        Description = claimSource.Description.ToHtmlString();
        ClaimApplyRules = claimSource.Project.Details.ClaimApplyRules.ToHtmlString();
        Fields = new CustomFieldsViewModel(playerUserId, claimSource, projectInfo, overrideValues);
        IsRoot = claimSource.IsRoot;
        return this;
    }

    public bool IsRoot { get; private set; }

    public bool CanSendClaim => ValidationStatus.Count == 0;

    public bool IsProjectRelatedReason { get; private set; }

    public bool ProjectAllowsMultipleCharacters { get; private set; }
}
