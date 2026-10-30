using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.ProjectMetadata;
using static JoinRpg.DomainTypes.Test.ProjectInfoFixture;

namespace JoinRpg.DomainTypes.Test.Characters;

/// <summary>
/// Сборка агрегата <see cref="CharacterInfo"/> и его заявок для тестов.
/// </summary>
internal static class CharacterInfoFixture
{
    public static readonly UserIdentification DefaultPlayerId = new(200);

    public static CharacterInfo MakeCharacter(
        ProjectInfo projectInfo,
        CharacterIdentification? id = null,
        CharacterTypeInfo? characterTypeInfo = null,
        FieldLayerContainer? characterFields = null,
        IReadOnlyCollection<CharacterGroupIdentification>? directGroupIds = null,
        IReadOnlyCollection<CharacterClaimInfo>? claims = null,
        ClaimIdentification? approvedClaimId = null,
        bool isActive = true,
        string name = "Вася")
        => new(
            id ?? new CharacterIdentification(ProjectId, 1),
            projectInfo,
            name,
            characterTypeInfo ?? CharacterTypeInfo.Default(),
            hidePlayerForCharacter: false,
            isActive: isActive,
            inGame: false,
            autoCreated: false,
            new MarkdownString(""),
            originalCharacterSlotId: null,
            directGroupIds ?? [],
            characterFields ?? new FieldLayerContainer(projectInfo, new Dictionary<int, string?>()),
            plotElementOrderData: null,
            claims ?? [],
            approvedClaimId,
            new DateTime(2024, 1, 1),
            DefaultMasterId,
            new DateTime(2024, 1, 2),
            DefaultMasterId);

    public static UserInfoHeader MakePlayer(UserIdentification userId)
        => new(userId, new UserDisplayName($"Игрок{userId.Value}", null), Avatar: null);

    public static CharacterClaimInfo MakeClaim(
        ProjectInfo projectInfo,
        int claimId,
        ClaimStatus status = ClaimStatus.AddedByUser,
        UserIdentification? playerId = null,
        UserIdentification? responsibleMasterId = null,
        Dictionary<int, string?>? fields = null,
        AccommodationTypeIdentification? accommodationTypeId = null,
        AccommodationGroupIdentification? accommodationGroupId = null)
        => new(
            new ClaimIdentification(ProjectId, claimId),
            MakePlayer(playerId ?? DefaultPlayerId),
            status,
            DenialStatus: null,
            responsibleMasterId ?? DefaultMasterId,
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 2),
            MasterAcceptedDate: null,
            MasterDeclinedDate: null,
            PlayerDeclinedDate: null,
            CheckInDate: null,
            LastPlayerCommentAt: null,
            LastMasterCommentAt: null,
            LastMasterCommentBy: null,
            LastVisibleMasterCommentAt: null,
            LastVisibleMasterCommentBy: null,
            CommentDiscussionId: new CommentDiscussionId(1),
            Finance: new ClaimFinanceInfo(
                FixedFee: null,
                PreferentialFeeUser: false,
                FeePaid: 0,
                AccommodationFee: 0,
                OperationsRequireModeration: false),
            AccommodationTypeId: accommodationTypeId,
            AccommodationGroupId: accommodationGroupId
                ?? AccommodationGroupIdentification.From(new ClaimIdentification(ProjectId, claimId)),
            PlayerAllowedSensitiveData: false,
            new FieldLayerContainer(projectInfo, fields ?? []));
}
