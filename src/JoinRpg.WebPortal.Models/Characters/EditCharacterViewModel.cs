using System.ComponentModel;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Extensions;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Web.Models.Characters;

public class EditCharacterViewModel : CharacterViewModelBase, ICreatedUpdatedTracked
{
    public int CharacterId { get; set; }

    [ReadOnly(true)]
    public CharacterNavigationViewModel Navigation { get; set; } = null!;

    [ReadOnly(true)]
    public bool IsActive { get; private set; }

    [ReadOnly(true)]
    public int ActiveClaimsCount { get; private set; }

    [ReadOnly(true)]
    public bool HasApprovedClaim { get; private set; }

    [ReadOnly(true)]
    public bool IsDefaultTemplate { get; private set; }

    public EditCharacterViewModel Fill(Character field, int currentUserId, ProjectInfo projectInfo)
    {
        Navigation = CharacterNavigationViewModel.FromCharacter(field,
            CharacterNavigationPage.Editing,
            currentUserId);
        FillFields(field, currentUserId, projectInfo);

        ActiveClaimsCount = field.Claims.Count(claim => claim.ClaimStatus.IsActive());
        IsActive = field.IsActive;
        HasApprovedClaim = field.ApprovedClaim is not null;

        CharacterTypeInfo = field.ToCharacterTypeInfo();

        CreatedAt = field.CreatedAt;
        UpdatedAt = field.UpdatedAt;
        CreatedBy = field.CreatedBy;
        UpdatedBy = field.UpdatedBy;

        IsDefaultTemplate = projectInfo.DefaultTemplateCharacter?.CharacterId == field.CharacterId;

        return this;
    }

    [ReadOnly(true)]
    public DateTime CreatedAt { get; private set; }

    [ReadOnly(true)]
    public User CreatedBy { get; private set; } = null!;

    [ReadOnly(true)]
    public DateTime UpdatedAt { get; private set; }

    [ReadOnly(true)]
    public User UpdatedBy { get; private set; } = null!;
}
