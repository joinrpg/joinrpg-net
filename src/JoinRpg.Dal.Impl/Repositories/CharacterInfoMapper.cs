using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

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
            row.PlotElementOrderData,
            [.. row.Claims.Select(claim => MapClaim(claim, projectInfo))],
            ClaimIdentification.FromOptional(projectId, row.ApprovedClaimId),
            row.CreatedAt,
            new UserIdentification(row.CreatedById),
            row.UpdatedAt,
            new UserIdentification(row.UpdatedById));
    }

    private static CharacterClaimInfo MapClaim(CharacterInfoClaimRow row, ProjectInfo projectInfo)
    {
        var accommodationTypeId = row.AccommodationTypeId is { } typeId
            ? new AccommodationTypeIdentification(projectInfo.ProjectId, typeId)
            : (AccommodationTypeIdentification?)null;

        // Стоимость проживания — настройка проекта, в метаданных она уже есть (ADR015), поэтому
        // в проекции её не читаем. Тип, выбранный в заявке, обязан быть в метаданных: в БД на
        // AccommodationRequest.AccommodationTypeId стоит FK, а метаданные отдают все типы проекта
        // без фильтров. Поэтому промах — это рассинхрон, и GetTypeById бросает
        // AccommodationTypeNotFoundException, а не подставляет 0: молчаливый ноль превратился бы
        // в заниженный взнос по заявке, который никто не заметит. Так же ведёт себя и легаси-путь
        // поверх EF — Claim.ClaimAccommodationFee через GetAccommodationType.
        var accommodationFee = accommodationTypeId is { } id
            ? projectInfo.AccommodationSettings.GetTypeById(id).Cost
            : 0;

        var claimId = new ClaimIdentification(projectInfo.ProjectId, row.ClaimId);

        // Ссылка на группу обязательна (ADR022): заявка вне группы — сама себе одиночная группа.
        var accommodationGroupId = row.AccommodationRequestId is { } requestId
            ? AccommodationGroupIdentification.From(new AccommodationRequestIdentification(projectInfo.ProjectId, requestId))
            : AccommodationGroupIdentification.From(claimId);

        return new(
            claimId,
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
            row.MasterAcceptedDate,
            row.MasterDeclinedDate,
            row.PlayerDeclinedDate,
            row.CheckInDate,
            row.LastPlayerCommentAt,
            row.LastMasterCommentAt,
            row.LastVisibleMasterCommentAt,
            new CommentDiscussionId(row.CommentDiscussionId),
            new ClaimFinanceInfo(
                row.CurrentFee,
                row.PreferentialFeeUser,
                row.FeePaid ?? 0,
                accommodationFee,
                row.FinanceOperationsRequireModeration),
            accommodationTypeId,
            accommodationGroupId,
            row.PlayerAllowedSensitiveData,
            FieldLayerContainer.DeserializeFieldLayer(projectInfo, row.JsonData));
    }
}
