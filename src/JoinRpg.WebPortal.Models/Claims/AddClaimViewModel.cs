using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.Markdown;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.PrimitiveTypes.Users;

namespace JoinRpg.Web.Models;

public class AddClaimViewModel : IProjectIdAware
{
    public int ProjectId { get; set; }

    public ProjectIdentification ProjectIdentification => new ProjectIdentification(ProjectId);
    public ProjectLifecycleStatus ProjectLifecycleStatus { get; private set; }

    public string ProjectName { get; set; }

    public JoinHtmlString ClaimApplyRules { get; set; }

    public int CharacterId { get; set; }

    [DisplayName("Заявка")]
    public string TargetName { get; set; }

    public bool IsSlot { get; set; }

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

    public static AddClaimViewModel Create(Character character, UserInfo userInfo, ProjectInfo projectInfo)
        => new AddClaimViewModel { CharacterId = character.CharacterId }.Fill(character, userInfo, projectInfo);

    public bool SenstiveDataRequired { get; private set; }

    [Display(Name = "Предоставить доступ к паспортным данным",
     Description = "Мастера игры просят вас предоставить доступ к паспортным данным. Вероятно, это нужно для поселения. Вы все равно сможете отправить заявку," +
        "даже если не предоставите доступ, но возможно, мастера отклонят вашу заявку. ")]
    public bool SensitiveDataAllowed { get; private set; }

    public AddClaimViewModel Fill(Character claimSource, UserInfo userInfo, ProjectInfo projectInfo, Dictionary<int, string?>? overrideValues = null)
    {
        var disallowReasons = claimSource.ValidateIfCanAddClaim(userInfo, projectInfo).ToList();

        IsProjectRelatedReason = disallowReasons.Intersect(
            [
                AddClaimForbideReason.ProjectClaimsClosed,
                AddClaimForbideReason.ProjectNotActive,
            ])
            .Any();

        ProjectLifecycleStatus = projectInfo.ProjectStatus;

        WarnForAnotherClaim = claimSource.Project.Claims.OfUserActive(userInfo.UserId.Value).Any();

        ValidationStatus = disallowReasons;
        ProjectAllowsMultipleCharacters = claimSource.Project.Details.EnableManyCharacters;

        ProjectId = claimSource.Project.ProjectId;
        ProjectName = claimSource.Project.ProjectName;
        TargetName = claimSource.CharacterName;
        IsSlot = claimSource.CharacterType == CharacterType.Slot;
        ClaimApplyRules = claimSource.Project.Details.ClaimApplyRules.ToHtmlString();
        var accessArguments = AccessArgumentsFactory.CreateForAdd(claimSource, userInfo.UserId);
        HasMasterAccess = accessArguments.MasterAccess;

        Fields = new CustomFieldsViewModel(claimSource, projectInfo, accessArguments.WithoutMasterAccess(), overrideValues: overrideValues);
        SenstiveDataRequired = projectInfo.ProfileRequirementSettings.SensitiveDataRequired;
        return this;
    }

    public bool CanSendClaim() => ValidationStatus.Count == 0;

    public bool IsProjectRelatedReason { get; private set; }

    public bool ProjectAllowsMultipleCharacters { get; private set; }

    public bool HasMasterAccess { get; private set; }
}
