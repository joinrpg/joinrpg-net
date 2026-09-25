using JoinRpg.Domain.Access;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Markdown;
// В JoinRpg.DataModel есть своя ProjectDetails (EF-сущность), здесь нужна доменная.
using ProjectDetails = JoinRpg.DomainTypes.ProjectMetadata.ProjectDetails;

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

    public IReadOnlyCollection<ClaimForbiddenReason> ValidationStatus
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

    public static AddClaimViewModel Create(
        CharacterInfo character,
        UserInfo userInfo,
        ProjectDetails projectDetails,
        ILinkRenderer renderer)
        => new AddClaimViewModel { CharacterId = character.Id.CharacterId }
            .Fill(character, userInfo, projectDetails, renderer);

    public bool SenstiveDataRequired { get; private set; }

    [Display(Name = "Предоставить доступ к паспортным данным",
     Description = "Мастера игры просят вас предоставить доступ к паспортным данным. Вероятно, это нужно для поселения. Вы все равно сможете отправить заявку," +
        " даже если не предоставите доступ, но возможно, мастера отклонят вашу заявку. ")]
    public bool SensitiveDataAllowed { get; set; }

    /// <param name="projectDetails">
    /// Нужны ради правил подачи заявки: они большие и в <see cref="ProjectInfo"/> не входят.
    /// <see cref="ProjectInfo"/> берётся из них же — это тот самый экземпляр, к которому привязан
    /// агрегат персонажа.
    /// </param>
    /// <param name="renderer">См. <see cref="CustomFieldsViewModel"/>: рендерер ссылок живёт на EF-проекте.</param>
    public AddClaimViewModel Fill(
        CharacterInfo claimSource,
        UserInfo userInfo,
        ProjectDetails projectDetails,
        ILinkRenderer renderer,
        Dictionary<int, string?>? overrideValues = null)
    {
        var projectInfo = claimSource.ProjectInfo;
        var disallowReasons = ClaimValidator.Validate(
            claimSource, userInfo, movedClaim: null, ClaimOperation.DisplayForPlayer);

        // Фатальная причина означает, что заявку тут не подать в принципе (проект в архиве или
        // приём заявок закрыт) — форму показывать незачем.
        IsProjectRelatedReason = disallowReasons.Any(r => r.IsFatal);

        ProjectLifecycleStatus = projectInfo.ProjectStatus;

        WarnForAnotherClaim = userInfo.ActiveClaims.Any(claim => claim.ProjectId == projectInfo.ProjectId);

        ValidationStatus = disallowReasons;
        ProjectAllowsMultipleCharacters = !projectInfo.ClaimSettings.StrictlyOneCharacter;

        ProjectId = projectInfo.ProjectId.Value;
        ProjectName = projectInfo.ProjectName;
        TargetName = claimSource.CharacterName;
        IsSlot = claimSource.CharacterType == CharacterType.Slot;
        ClaimApplyRules = projectDetails.ClaimApplyRules.ToHtmlString();
        var accessArguments = AccessArgumentsFactory.CreateForAdd(claimSource, userInfo.UserId);
        HasMasterAccess = accessArguments.MasterAccess;

        Fields = new CustomFieldsViewModel(claimSource, accessArguments.WithoutMasterAccess(), renderer, overrideValues);
        SenstiveDataRequired = projectInfo.ProfileRequirementSettings.SensitiveDataRequired;
        return this;
    }

    public bool CanSendClaim() => ValidationStatus.Count == 0;

    public bool IsProjectRelatedReason { get; private set; }

    public bool ProjectAllowsMultipleCharacters { get; private set; }

    public bool HasMasterAccess { get; private set; }
}
