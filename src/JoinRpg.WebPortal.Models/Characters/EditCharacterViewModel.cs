using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Web.Models.Characters;

public class EditCharacterViewModel : CharacterViewModelBase
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

    public EditCharacterViewModel Fill(
        Character field,
        CharacterInfo characterInfo,
        UserIdentification currentUserId,
        ProjectInfo projectInfo,
        IReadOnlyDictionary<UserIdentification, UserInfoHeader> fieldUsers)
    {
        Navigation = CharacterNavigationViewModel.FromCharacter(characterInfo,
            CharacterNavigationPage.Editing,
            currentUserId);
        FillFields(field, currentUserId, projectInfo, fieldUsers);

        ActiveClaimsCount = field.Claims.Count(claim => claim.ClaimStatus.IsActive());
        IsActive = field.IsActive;
        HasApprovedClaim = field.ApprovedClaim is not null;

        CharacterTypeInfo = field.ToCharacterTypeInfo();

        Marks = field.ToCreateUpdateMarksViewModel();

        IsDefaultTemplate = projectInfo.ClaimSettings.DefaultTemplate?.CharacterId == field.CharacterId;

        return this;
    }

    /// <summary>Кто и когда создал и последним менял персонажа.</summary>
    [ReadOnly(true)]
    public CreateUpdateMarksViewModel Marks { get; private set; } = null!;
}
