using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models.Characters
{
    public abstract class CharacterViewModelBase : GameObjectViewModelBase, IValidatableObject
    {
        [Display(Name = "Принимать заявки на этого персонажа",
            Description =
                "Разрешает игрокам подавать заявки на этого персонажа. Снимите галочку для NPC.")]
        public bool IsAcceptingClaims { get; set; } = true;

        [DisplayName("Имя персонажа"), Required]
        public string Name { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!ParentCharacterGroupIds.Any())
            {
                yield return new ValidationResult(
                    "Персонаж должен принадлежать хотя бы к одной группе");
            }

            if (!IsAcceptingClaims && IsHot)
            {
                yield return new ValidationResult("На горячую роль должны приниматься заявки");
            }
        }

        [Display(Name = "Всегда скрывать имя игрока",
            Description = "Скрыть личность игрока, который играет данного персонажа.")]
        public bool HidePlayerForCharacter { get; set; }

        [Display(Name = "Горячая роль",
            Description = "Горячая роль специальным образом выделяется в ролевке.")]
        public bool IsHot { get; set; }

        public CustomFieldsViewModel Fields { get; set; }

        protected void FillFields(Character field, int currentUserId)
        {
            Fields = new CustomFieldsViewModel(currentUserId, field);
        }
    }

    public class AddCharacterViewModel : CharacterViewModelBase
    {
        public AddCharacterViewModel Fill(CharacterGroup characterGroup, int currentUserId)
        {
            ProjectId = characterGroup.ProjectId;
            FillFields(new Character()
            {
                Project = characterGroup.Project,
                ProjectId =  ProjectId,
                IsAcceptingClaims = true,
                ParentCharacterGroupIds = new [] {characterGroup.CharacterGroupId},
            }, currentUserId);
            return this;
        }

        [Display(Name="Добавить еще одного персонажа", Description ="После сохранения продолжить добавлять персонажей в эту группу")]
        public bool ContinueCreating { get; set; }
    }

    public class EditCharacterViewModel : CharacterViewModelBase, ICreatedUpdatedTracked
    {
        public int CharacterId { get; set; }

        public CharacterNavigationViewModel Navigation { get; set; }

        [ReadOnly(true)]
        public bool IsActive { get; private set; }

        [ReadOnly(true)]
        public int ActiveClaimsCount { get; private set; }

        [ReadOnly(true)]
        public bool IsAcceptingClaimsEnabled { get; private set; }

        public EditCharacterViewModel Fill(Character field, int currentUserId)
        {
            Navigation = CharacterNavigationViewModel.FromCharacter(field,
                CharacterNavigationPage.Editing,
                currentUserId);
            FillFields(field, currentUserId);

            ActiveClaimsCount = field.Claims.Count(claim => claim.ClaimStatus.IsActive());
            IsActive = field.IsActive;
            IsAcceptingClaimsEnabled = field.ApprovedClaim == null;

            IsAcceptingClaims = IsAcceptingClaims && IsAcceptingClaimsEnabled;
            CreatedAt = field.CreatedAt;
            UpdatedAt = field.UpdatedAt;
            CreatedBy = field.CreatedBy;
            UpdatedBy = field.UpdatedBy;

            return this;
        }

        public DateTime CreatedAt { get; private set; }
        public User CreatedBy { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        public User UpdatedBy { get; private set; }
    }

    public enum CharacterNavigationPage
    {
        None,
        Character,
        Editing,
        Claim,
        RejectedClaim,
        AddClaim,
    }

    /// <summary>
    /// TODO: LEO describe the meaning of this tricky class properly
    /// </summary>
    public class CharacterNavigationViewModel
    {
        public CharacterNavigationPage Page { get; private set; }
        public bool HasMasterAccess { get; private set; }
        public bool CanEditRoles { get; private set; }

        public bool CanAddClaim { get; private set; }
        public int? ClaimId { get; private set; }
        public int? CharacterId { get; private set; }
        public int ProjectId { get; private set; }

        public string Name { get; private set; }

        [ReadOnly(true)]
        public bool IsActive { get; private set; }

        public IEnumerable<ClaimShortListItemViewModel> DiscussedClaims { get; private set; }
        public IEnumerable<ClaimShortListItemViewModel> RejectedClaims { get; private set; }

        public static CharacterNavigationViewModel FromCharacter(Character character,
            CharacterNavigationPage page,
            int? currentUserId)
        {
            int? claimId;

            if (character.ApprovedClaim?.HasAccess(currentUserId, ExtraAccessReason.Player) == true
            ) //If Approved Claim exists and we have access to it, so be it.
            {
                claimId = character.ApprovedClaim.ClaimId;
            }
            else // if we have My claims, try select single one. We may fail to do so.
            {
                claimId = character.Claims.Where(c => c.PlayerUserId == currentUserId)
                    .TrySelectSingleClaim()?.ClaimId;
            }

            var vm = new CharacterNavigationViewModel
            {
                CanAddClaim = character.IsAvailable,
                ClaimId = claimId,
                HasMasterAccess = character.HasMasterAccess(currentUserId),
                CanEditRoles = character.HasEditRolesAccess(currentUserId),
                CharacterId = character.CharacterId,
                ProjectId = character.ProjectId,
                Page = page,
                Name = character.CharacterName,
                IsActive = character.IsActive,
            };

            vm.LoadClaims(character);
            return vm;
        }

        private void LoadClaims(Character field)
        {
            RejectedClaims = LoadClaimsWithCondition(field, claim => !claim.ClaimStatus.IsActive());
            DiscussedClaims = LoadClaimsWithCondition(field, claim => claim.IsInDiscussion);
        }

        private IEnumerable<ClaimShortListItemViewModel> LoadClaimsWithCondition(Character field,
            Func<Claim, bool> predicate)
        {
            return HasMasterAccess && field != null
                ? field.Claims.Where(predicate)
                    .Select(claim => new ClaimShortListItemViewModel(claim))
                : Enumerable.Empty<ClaimShortListItemViewModel>();
        }

        public static CharacterNavigationViewModel FromClaim([NotNull]
            Claim claim,
            int currentUserId,
            CharacterNavigationPage characterNavigationPage)
        {
            if (claim == null) throw new ArgumentNullException(nameof(claim));
            var vm = new CharacterNavigationViewModel
            {
                CanAddClaim = false,
                ClaimId = claim.ClaimId,
                HasMasterAccess = claim.HasMasterAccess(currentUserId),
                CharacterId = claim.Character?.CharacterId,
                ProjectId = claim.ProjectId,
                Page = characterNavigationPage,
                Name = claim.GetTarget().Name,
                CanEditRoles = claim.HasEditRolesAccess(currentUserId),
                IsActive = claim.GetTarget().IsActive,
            };
            vm.LoadClaims(claim.Character);
            if (vm.RejectedClaims.Any(c => c.ClaimId == claim.ClaimId))
            {
                vm.Page = CharacterNavigationPage.RejectedClaim;
                vm.ClaimId = null;
            }

            return vm;
        }
    }
}
