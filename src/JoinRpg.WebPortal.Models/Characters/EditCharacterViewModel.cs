using System.ComponentModel;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Extensions;

namespace JoinRpg.Web.Models.Characters;

public class EditCharacterViewModel : CharacterViewModelBase, ICreatedUpdatedTracked
{
    public int CharacterId { get; set; }

    public CharacterNavigationViewModel Navigation { get; set; }

    [ReadOnly(true)]
    public bool IsActive { get; private set; }

    [ReadOnly(true)]
    public int ActiveClaimsCount { get; private set; }

    [ReadOnly(true)]
    public bool HasApprovedClaim { get; private set; }

    public EditCharacterViewModel Fill(Character field, int currentUserId)
    {
        Navigation = CharacterNavigationViewModel.FromCharacter(field,
            CharacterNavigationPage.Editing,
            currentUserId);
        FillFields(field, currentUserId);

        ActiveClaimsCount = field.Claims.Count(claim => claim.ClaimStatus.IsActive());
        IsActive = field.IsActive;
        HasApprovedClaim = field.ApprovedClaim is not null;

        CharacterTypeInfo = field.ToCharacterTypeInfo();

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
