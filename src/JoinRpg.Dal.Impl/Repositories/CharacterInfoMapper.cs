using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Dal.Impl.Repositories;

/// <summary>
/// Чистое преобразование плоской проекции из БД в доменный агрегат (ADR013).
/// Вынесено из репозитория, чтобы покрываться юнит-тестами без базы.
/// </summary>
internal static class CharacterInfoMapper
{
    public static CharacterInfo Map(CharacterInfoRow row, ProjectInfo projectInfo)
    {
        var projectId = projectInfo.ProjectId;

        return new CharacterInfo(
            new CharacterIdentification(projectId, row.CharacterId),
            projectInfo,
            row.CharacterName,
            // Маппинг флагов живёт в одном месте — иначе он разойдётся с ToCharacterTypeInfo.
            CharacterTypeInfo.Create(
                row.CharacterType,
                row.IsHot,
                row.CharacterSlotLimit,
                row.CharacterName,
                row.IsPublic,
                row.HidePlayerForCharacter),
            row.HidePlayerForCharacter,
            row.IsActive,
            row.InGame,
            row.AutoCreated,
            new MarkdownString(row.Description?.Contents ?? ""),
            CharacterIdentification.FromOptional(projectId, row.OriginalCharacterSlotId),
            [.. row.ParentGroups._parentCharacterGroupIds.Select(id => new CharacterGroupIdentification(projectId, id))],
            FieldLayerContainer.DeserializeFieldLayer(projectInfo, row.JsonData),
            [.. row.Claims.Select(claim => MapClaim(claim, projectInfo))],
            ClaimIdentification.FromOptional(projectId, row.ApprovedClaimId),
            row.CreatedAt,
            new UserIdentification(row.CreatedById),
            row.UpdatedAt,
            new UserIdentification(row.UpdatedById));
    }

    private static CharacterClaimInfo MapClaim(CharacterInfoClaimRow row, ProjectInfo projectInfo)
        => new(
            new ClaimIdentification(projectInfo.ProjectId, row.ClaimId),
            new UserInfoHeader(
                new UserIdentification(row.PlayerUserId),
                new UserDisplayName(
                    new UserFullName(
                        PrefferedName.FromOptional(row.PlayerPrefferedName),
                        BornName.FromOptional(row.PlayerBornName),
                        SurName.FromOptional(row.PlayerSurName),
                        FatherName.FromOptional(row.PlayerFatherName)),
                    new Email(row.PlayerEmail))),
            row.ClaimStatus,
            row.ClaimDenialStatus,
            new UserIdentification(row.ResponsibleMasterUserId),
            row.CreateDate,
            row.LastUpdateDateTime,
            row.CheckInDate,
            row.LastPlayerCommentAt,
            row.LastMasterCommentAt,
            row.LastVisibleMasterCommentAt,
            row.CurrentFee,
            row.PreferentialFeeUser,
            row.FeePaid ?? 0,
            row.AccommodationFee ?? 0,
            FieldLayerContainer.DeserializeFieldLayer(projectInfo, row.JsonData));
}
