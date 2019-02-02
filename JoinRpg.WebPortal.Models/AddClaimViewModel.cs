using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Joinrpg.Markdown;
using JoinRpg.CommonUI.Models;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers.Web;

namespace JoinRpg.Web.Models
{
    public class AddClaimViewModel
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

        public bool IsAvailable { get; set; }

        public IReadOnlyCollection<AddClaimForbideReasonViewModel> ValidationStatus
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

        public static AddClaimViewModel Create(Character character, int playerUserId)
            => new AddClaimViewModel {CharacterId = character.CharacterId}.Fill(character, playerUserId);

        public static AddClaimViewModel Create(CharacterGroup group, int playerUserId)
            => new AddClaimViewModel {CharacterGroupId = group.CharacterGroupId}.Fill(group, playerUserId);

        public AddClaimViewModel Fill(IClaimSource claimSource, int playerUserId)
        {
            var disallowReasons = claimSource.ValidateIfCanAddClaim(playerUserId)
                .Select(x => x.ToViewModel()).ToList();

            CanSendClaim = !disallowReasons.Any();

            IsProjectRelatedReason = disallowReasons.Intersect(new[]
                {
                    AddClaimForbideReasonViewModel.ProjectClaimsClosed,
                    AddClaimForbideReasonViewModel.ProjectNotActive,
                })
                .Any();

            

            if (!disallowReasons.Any())
            {
                var myClaims = claimSource.Project.Claims.OfUserActive(playerUserId);
                if (myClaims.Any())
                {
                    disallowReasons.Add(AddClaimForbideReasonViewModel
                        .AlredySentNotApprovedClaimToAnotherPlace);
                }
            }

            ValidationStatus = disallowReasons;
            ProjectAllowsMultipleCharacters = claimSource.Project.Details.EnableManyCharacters;

            ProjectId = claimSource.Project.ProjectId;
            ProjectName = claimSource.Project.ProjectName;
            TargetName = claimSource.Name;
            Description = claimSource.Description.ToHtmlString();
            IsAvailable = claimSource.IsAvailable;
            ClaimApplyRules = claimSource.Project.Details.ClaimApplyRules.ToHtmlString();
            Fields = new CustomFieldsViewModel(playerUserId, claimSource);
            IsRoot = claimSource.IsRoot;
            return this;
        }

        public bool IsRoot { get; private set; }

        public bool CanSendClaim { get; private set; }

        public bool IsProjectRelatedReason { get; private set; }

        public bool ProjectAllowsMultipleCharacters { get; private set; }
    }
}
