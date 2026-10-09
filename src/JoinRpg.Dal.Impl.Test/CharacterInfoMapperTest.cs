using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Extensions;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Test;

/// <summary>
/// Юнит-тесты чистого маппинга <see cref="CharacterInfoMapper.Map"/> (ADR013, п.7).
/// БД не используется — только in-memory проекции.
/// </summary>
public class CharacterInfoMapperTest
{
    private readonly MockedProject _mock = new();
    private ProjectInfo ProjectInfo => _mock.ProjectInfo;
    private ProjectIdentification ProjectId => ProjectInfo.ProjectId;

    private static readonly DateTime SomeDate = new(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Строит строку персонажа с разумными дефолтами — тест задаёт только интересующее его поле.
    /// </summary>
    private CharacterInfoRow MakeRow(
        int characterId = 1,
        string characterName = "Персонаж",
        CharacterType characterType = CharacterType.Player,
        bool isHot = false,
        int? characterSlotLimit = null,
        bool isPublic = true,
        bool hidePlayerForCharacter = false,
        bool isActive = true,
        bool inGame = true,
        bool autoCreated = false,
        string? jsonData = null,
        MarkdownDbValue? description = null,
        IntList? parentGroups = null,
        string? plotElementOrderData = null,
        int? approvedClaimId = null,
        int? originalCharacterSlotId = null,
        DateTime? createdAt = null,
        int createdById = 2,
        DateTime? updatedAt = null,
        int updatedById = 2,
        IEnumerable<CharacterInfoClaimRow>? claims = null)
        => new()
        {
            CharacterId = characterId,
            CharacterName = characterName,
            CharacterType = characterType,
            IsHot = isHot,
            CharacterSlotLimit = characterSlotLimit,
            IsPublic = isPublic,
            HidePlayerForCharacter = hidePlayerForCharacter,
            IsActive = isActive,
            InGame = inGame,
            AutoCreated = autoCreated,
            JsonData = jsonData,
            Description = description ?? new MarkdownDbValue(null),
            ParentGroups = parentGroups ?? new IntList { ListIds = "" },
            PlotElementOrderData = plotElementOrderData,
            ApprovedClaimId = approvedClaimId,
            OriginalCharacterSlotId = originalCharacterSlotId,
            CreatedAt = createdAt ?? SomeDate,
            CreatedById = createdById,
            UpdatedAt = updatedAt ?? SomeDate,
            UpdatedById = updatedById,
            Claims = claims ?? [],
        };

    /// <summary>Строит строку заявки с разумными дефолтами. По умолчанию заявка неактивной утверждённой не является.</summary>
    private static CharacterInfoClaimRow MakeClaimRow(
        int claimId = 1,
        int playerUserId = 1,
        string? playerPrefferedName = null,
        string? playerBornName = null,
        string? playerSurName = null,
        string? playerFatherName = null,
        string playerEmail = "player@example.com",
        ClaimStatus claimStatus = ClaimStatus.AddedByUser,
        ClaimDenialReason? claimDenialStatus = null,
        int responsibleMasterUserId = 2,
        DateTime? createDate = null,
        DateTime? lastUpdateDateTime = null,
        DateTime? masterAcceptedDate = null,
        DateTime? masterDeclinedDate = null,
        DateTime? playerDeclinedDate = null,
        DateTime? checkInDate = null,
        DateTimeOffset? lastPlayerCommentAt = null,
        DateTimeOffset? lastMasterCommentAt = null,
        DateTimeOffset? lastVisibleMasterCommentAt = null,
        int? currentFee = null,
        bool preferentialFeeUser = false,
        string? jsonData = null,
        int? feePaid = null,
        int? accommodationTypeId = null,
        int? accommodationRequestId = null,
        bool financeOperationsRequireModeration = false,
        bool playerAllowedSensitiveData = false)
        => new()
        {
            ClaimId = claimId,
            PlayerUserId = playerUserId,
            PlayerPrefferedName = playerPrefferedName,
            PlayerBornName = playerBornName,
            PlayerSurName = playerSurName,
            PlayerFatherName = playerFatherName,
            PlayerEmail = playerEmail,
            ClaimStatus = claimStatus,
            ClaimDenialStatus = claimDenialStatus,
            ResponsibleMasterUserId = responsibleMasterUserId,
            CreateDate = createDate ?? SomeDate,
            LastUpdateDateTime = lastUpdateDateTime ?? SomeDate,
            MasterAcceptedDate = masterAcceptedDate,
            MasterDeclinedDate = masterDeclinedDate,
            PlayerDeclinedDate = playerDeclinedDate,
            CheckInDate = checkInDate,
            LastPlayerCommentAt = lastPlayerCommentAt,
            LastMasterCommentAt = lastMasterCommentAt,
            LastVisibleMasterCommentAt = lastVisibleMasterCommentAt,
            CommentDiscussionId = 0,
            CurrentFee = currentFee,
            PreferentialFeeUser = preferentialFeeUser,
            JsonData = jsonData,
            FeePaid = feePaid,
            AccommodationTypeId = accommodationTypeId,
            // Тип хранится только в строке группы, поэтому заявка с типом всегда в группе — как в БД.
            AccommodationRequestId = accommodationRequestId ?? (accommodationTypeId is null ? null : 77),
            FinanceOperationsRequireModeration = financeOperationsRequireModeration,
            PlayerAllowedSensitiveData = playerAllowedSensitiveData,
        };

    // 1. ParentGroups: непустой ListIds -> DirectGroupIds из соответствующих групп того же проекта.

    [Fact]
    public void Map_ParentGroups_ShouldProduceDirectGroupIdsForEachListedId()
    {
        var row = MakeRow(parentGroups: new IntList { ListIds = "1,2,3" });

        var result = CharacterInfoMapper.Map(row, ProjectInfo);

        result.DirectGroupIds.ShouldBe(
            [
                new CharacterGroupIdentification(ProjectId, 1),
                new CharacterGroupIdentification(ProjectId, 2),
                new CharacterGroupIdentification(ProjectId, 3),
            ],
            ignoreOrder: true);
    }

    // 2. Пустой ListIds -> DirectGroupIds пуст.

    [Fact]
    public void Map_ParentGroups_EmptyListIds_ShouldProduceEmptyDirectGroupIds()
    {
        var row = MakeRow(parentGroups: new IntList { ListIds = "" });

        var result = CharacterInfoMapper.Map(row, ProjectInfo);

        result.DirectGroupIds.ShouldBeEmpty();
    }

    // 3. JsonData == null и JsonData == "" -> пустой слой полей, без исключений (для персонажа и для заявки).

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Map_CharacterJsonDataNullOrEmpty_ShouldProduceEmptyFieldLayerWithoutException(string? jsonData)
    {
        var row = MakeRow(jsonData: jsonData);

        var result = CharacterInfoMapper.Map(row, ProjectInfo);

        result.CharacterFields.LayerData.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Map_ClaimJsonDataNullOrEmpty_ShouldProduceEmptyFieldLayerWithoutException(string? jsonData)
    {
        var row = MakeRow(claims: [MakeClaimRow(jsonData: jsonData)]);

        var result = CharacterInfoMapper.Map(row, ProjectInfo);

        result.Claims.Single().Fields.LayerData.ShouldBeEmpty();
    }

    // 4. FeePaid == null -> 0, проживание не выбрано -> AccommodationFee 0;
    // ненулевой FeePaid проходит как есть.

    [Fact]
    public void Map_ClaimFeePaidNullAndNoAccommodation_ShouldMapToZero()
    {
        var row = MakeRow(claims: [MakeClaimRow(feePaid: null, accommodationTypeId: null)]);

        var result = CharacterInfoMapper.Map(row, ProjectInfo);

        var claim = result.Claims.Single();
        claim.Finance.FeePaid.ShouldBe(0);
        claim.Finance.AccommodationFee.ShouldBe(0);
    }

    [Fact]
    public void Map_ClaimFeePaidSet_ShouldPassThroughAsIs()
    {
        var row = MakeRow(claims: [MakeClaimRow(feePaid: 1500)]);

        var result = CharacterInfoMapper.Map(row, ProjectInfo);

        result.Claims.Single().Finance.FeePaid.ShouldBe(1500);
    }

    // 4в. Стоимость проживания приезжает не из проекции, а из метаданных проекта по id типа
    // (ADR015): в строке заявки колонки со стоимостью нет вовсе.

    [Fact]
    public void Map_ClaimAccommodationFee_ShouldComeFromProjectMetadata()
    {
        var accommodationType = _mock.CreateAccommodationType(cost: 1234);
        _mock.ReInitProjectInfo();

        var row = MakeRow(claims: [MakeClaimRow(accommodationTypeId: accommodationType.Id)]);

        var result = CharacterInfoMapper.Map(row, ProjectInfo);

        result.Claims.Single().Finance.AccommodationFee.ShouldBe(1234);
    }

    [Fact]
    public void Map_ClaimAccommodationFee_ShouldFollowTheTypeChosenInClaim()
    {
        // Страж от «берём первый тип подряд»: типов в проекте несколько, цена должна совпасть
        // именно с выбранным в заявке.
        var cheap = _mock.CreateAccommodationType("Палатка", cost: 100);
        var pricey = _mock.CreateAccommodationType("Домик", cost: 900);
        _mock.ReInitProjectInfo();

        int FeeFor(ProjectAccommodationType type)
            => CharacterInfoMapper
                .Map(MakeRow(claims: [MakeClaimRow(accommodationTypeId: type.Id)]), ProjectInfo)
                .Claims.Single().Finance.AccommodationFee;

        FeeFor(cheap).ShouldBe(100);
        FeeFor(pricey).ShouldBe(900);
    }

    [Fact]
    public void Map_ClaimAccommodationTypeMissingInMetadata_ShouldThrow()
    {
        // Решение по ненайденному типу: исключение, а не 0 — см. комментарий в CharacterInfoMapper.
        var row = MakeRow(claims: [MakeClaimRow(accommodationTypeId: 12345)]);

        Should.Throw<AccommodationTypeNotFoundException>(
            () => CharacterInfoMapper.Map(row, ProjectInfo));
    }

    // 4а. Флаги заявки, нужные фильтрам проблем, переносятся как есть — без инверсий и дефолтов.

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Map_ClaimFinanceOperationsRequireModeration_ShouldPassThroughAsIs(bool requireModeration)
    {
        var row = MakeRow(claims: [MakeClaimRow(financeOperationsRequireModeration: requireModeration)]);

        var result = CharacterInfoMapper.Map(row, ProjectInfo);

        result.Claims.Single().Finance.OperationsRequireModeration.ShouldBe(requireModeration);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Map_ClaimPlayerAllowedSensitiveData_ShouldPassThroughAsIs(bool allowed)
    {
        var row = MakeRow(claims: [MakeClaimRow(playerAllowedSensitiveData: allowed)]);

        var result = CharacterInfoMapper.Map(row, ProjectInfo);

        result.Claims.Single().PlayerAllowedSensitiveData.ShouldBe(allowed);
    }

    // 4б. Тип проживания: id из заявки превращается в типизированный id того же проекта,
    // а «проживание не выбрано» остаётся null — на нём строится «Поселение: нет» в конверте.

    [Fact]
    public void Map_ClaimAccommodationTypeIdSet_ShouldMapToProjectScopedId()
    {
        var accommodationType = _mock.CreateAccommodationType();
        _mock.ReInitProjectInfo();

        var row = MakeRow(claims: [MakeClaimRow(accommodationTypeId: accommodationType.Id)]);

        var result = CharacterInfoMapper.Map(row, ProjectInfo);

        result.Claims.Single().AccommodationTypeId
            .ShouldBe(new AccommodationTypeIdentification(ProjectInfo.ProjectId, accommodationType.Id));
    }

    [Fact]
    public void Map_ClaimAccommodationTypeIdNull_ShouldMapToNull()
    {
        var row = MakeRow(claims: [MakeClaimRow(accommodationTypeId: null)]);

        var result = CharacterInfoMapper.Map(row, ProjectInfo);

        result.Claims.Single().AccommodationTypeId.ShouldBeNull();
    }

    // 4в. Ссылка на группу проживающих (ADR022): обязательная — группа либо сама заявка-одиночка.

    [Fact]
    public void Map_ClaimInAccommodationGroup_ShouldReferenceGroup()
    {
        var accommodationType = _mock.CreateAccommodationType();
        _mock.ReInitProjectInfo();

        var row = MakeRow(claims:
            [MakeClaimRow(claimId: 5, accommodationTypeId: accommodationType.Id, accommodationRequestId: 42)]);

        var result = CharacterInfoMapper.Map(row, ProjectInfo);

        var groupId = result.Claims.Single().AccommodationGroupId;
        groupId.AsAccommodationRequestId().ShouldBe(new AccommodationRequestIdentification(ProjectId, 42));
        groupId.AsClaimId().ShouldBeNull();
    }

    [Fact]
    public void Map_ClaimWithoutAccommodationGroup_ShouldReferenceItself()
    {
        var row = MakeRow(claims: [MakeClaimRow(claimId: 5, accommodationTypeId: null)]);

        var result = CharacterInfoMapper.Map(row, ProjectInfo);

        result.Claims.Single().AccommodationGroupId
            .ShouldBe(AccommodationGroupIdentification.From(new ClaimIdentification(ProjectId, 5)));
    }

    // 5. Description == null и Description с null Contents -> пустая MarkdownString, без исключений.

    [Fact]
    public void Map_DescriptionIsNull_ShouldProduceEmptyMarkdownString()
    {
        var row = MakeRow(description: null!);

        var result = CharacterInfoMapper.Map(row, ProjectInfo);

        result.Description.ShouldBe(new MarkdownString(""));
    }

    [Fact]
    public void Map_DescriptionHasNullContents_ShouldProduceEmptyMarkdownString()
    {
        var row = MakeRow(description: new MarkdownDbValue(null));

        var result = CharacterInfoMapper.Map(row, ProjectInfo);

        result.Description.ShouldBe(new MarkdownString(""));
    }

    // 5a. PlotElementOrderData переносится как есть — домен в этот блоб не заглядывает.

    [Fact]
    public void Map_PlotElementOrderData_ShouldBeCopiedAsIs()
    {
        var row = MakeRow(plotElementOrderData: "3,1,2");

        var result = CharacterInfoMapper.Map(row, ProjectInfo);

        result.PlotElementOrderData.ShouldBe("3,1,2");
    }

    [Fact]
    public void Map_PlotElementOrderDataIsNull_ShouldStayNull()
    {
        var row = MakeRow(plotElementOrderData: null);

        var result = CharacterInfoMapper.Map(row, ProjectInfo);

        result.PlotElementOrderData.ShouldBeNull();
    }

    // 6. ApprovedClaimId/OriginalCharacterSlotId == null -> null; ненулевые -> корректные типизированные Id.

    [Fact]
    public void Map_ApprovedClaimIdAndOriginalCharacterSlotIdNull_ShouldMapToNull()
    {
        var row = MakeRow(approvedClaimId: null, originalCharacterSlotId: null);

        var result = CharacterInfoMapper.Map(row, ProjectInfo);

        result.ApprovedClaimId.ShouldBeNull();
        result.OriginalCharacterSlotId.ShouldBeNull();
    }

    [Fact]
    public void Map_ApprovedClaimIdSet_ShouldMapToTypedClaimIdentification()
    {
        var row = MakeRow(
            approvedClaimId: 42,
            claims: [MakeClaimRow(claimId: 42, claimStatus: ClaimStatus.Approved)]);

        var result = CharacterInfoMapper.Map(row, ProjectInfo);

        result.ApprovedClaimId.ShouldBe(new ClaimIdentification(ProjectId, 42));
    }

    [Fact]
    public void Map_OriginalCharacterSlotIdSet_ShouldMapToTypedCharacterIdentification()
    {
        var row = MakeRow(originalCharacterSlotId: 7);

        var result = CharacterInfoMapper.Map(row, ProjectInfo);

        result.OriginalCharacterSlotId.ShouldBe(new CharacterIdentification(ProjectId, 7));
    }

    // 7. ТЕСТ-СТРАЖ: маппинг (IsPublic, HidePlayerForCharacter) должен совпадать с Character.ToCharacterTypeInfo().

    [Theory]
    // Player: комбинации флагов видимости
    [InlineData(CharacterType.Player, false, false)]
    [InlineData(CharacterType.Player, false, true)]
    [InlineData(CharacterType.Player, true, false)]
    [InlineData(CharacterType.Player, true, true)]
    // NonPlayer и Slot: у слота имя персонажа становится SlotName — этот путь тоже должен совпадать
    [InlineData(CharacterType.NonPlayer, true, false)]
    [InlineData(CharacterType.Slot, true, false)]
    public void Map_CharacterTypeInfo_ShouldMatchCharacterToCharacterTypeInfoExtension(
        CharacterType characterType,
        bool isPublic,
        bool hidePlayerForCharacter)
    {
        const string name = "Персонаж-страж";
        // NPC не может быть горячим, лимит есть только у слота — иначе ctor CharacterTypeInfo бросит.
        var isHot = characterType != CharacterType.NonPlayer;
        int? slotLimit = characterType == CharacterType.Slot ? 3 : null;

        var row = MakeRow(
            characterName: name,
            characterType: characterType,
            isHot: isHot,
            characterSlotLimit: slotLimit,
            isPublic: isPublic,
            hidePlayerForCharacter: hidePlayerForCharacter);

        var mapped = CharacterInfoMapper.Map(row, ProjectInfo);

        var character = _mock.CreateCharacter(name);
        character.CharacterType = characterType;
        character.IsHot = isHot;
        character.CharacterSlotLimit = slotLimit;
        character.IsPublic = isPublic;
        character.HidePlayerForCharacter = hidePlayerForCharacter;

        var fromExtension = character.ToCharacterTypeInfo();

        mapped.CharacterTypeInfo.ShouldBe(fromExtension);
    }

    // 8. ТЕСТ-СТРАЖ: ClaimPredicates.GetClaimStatusPredicate(Active) должен совпадать с CharacterClaimInfo.IsActive
    // для всех значений ClaimStatus.

    [Theory]
    [InlineData(ClaimStatus.AddedByUser)]
    [InlineData(ClaimStatus.AddedByMaster)]
    [InlineData(ClaimStatus.Approved)]
    [InlineData(ClaimStatus.DeclinedByUser)]
    [InlineData(ClaimStatus.DeclinedByMaster)]
    [InlineData(ClaimStatus.Discussed)]
    [InlineData(ClaimStatus.OnHold)]
    [InlineData(ClaimStatus.CheckedIn)]
    public void Map_ClaimIsActive_ShouldMatchSqlActivePredicate(ClaimStatus status)
    {
        var row = MakeRow(claims: [MakeClaimRow(claimStatus: status)]);
        var mapped = CharacterInfoMapper.Map(row, ProjectInfo);
        var mappedClaim = mapped.Claims.Single();

        var dbClaim = new JoinRpg.DataModel.Claim { ClaimStatus = status };
        var predicate = ClaimPredicates.GetClaimStatusPredicate(ClaimStatusSpec.Active).Compile();

        mappedClaim.IsActive.ShouldBe(predicate(dbClaim));
    }

    // 9а. Персонаж: базовые поля переносятся один в один.

    [Fact]
    public void Map_CharacterBasicFields_ShouldBeTransferredAsIs()
    {
        var createdAt = new DateTime(2024, 2, 1, 9, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2024, 2, 5, 9, 0, 0, DateTimeKind.Utc);

        var row = MakeRow(
            characterId: 77,
            characterName: "Пётр",
            isActive: false,
            inGame: true,
            autoCreated: true,
            hidePlayerForCharacter: true,
            description: new MarkdownDbValue("**описание**"),
            createdAt: createdAt,
            createdById: 11,
            updatedAt: updatedAt,
            updatedById: 12);

        var result = CharacterInfoMapper.Map(row, ProjectInfo);

        result.Id.ShouldBe(new CharacterIdentification(ProjectId, 77));
        result.ProjectInfo.ShouldBeSameAs(ProjectInfo);
        result.CharacterName.ShouldBe("Пётр");
        result.IsActive.ShouldBeFalse();
        result.InGame.ShouldBeTrue();
        result.AutoCreated.ShouldBeTrue();
        result.HidePlayerForCharacter.ShouldBeTrue();
        result.Description.ShouldBe(new MarkdownString("**описание**"));
        result.CreatedAt.ShouldBe(createdAt);
        result.CreatedById.ShouldBe(new UserIdentification(11));
        result.UpdatedAt.ShouldBe(updatedAt);
        result.UpdatedById.ShouldBe(new UserIdentification(12));
    }

    // 9б. Заявки: базовые поля переносятся один в один.

    [Fact]
    public void Map_ClaimBasicFields_ShouldBeTransferredAsIs()
    {
        var createDate = new DateTime(2024, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var lastUpdate = new DateTime(2024, 3, 2, 11, 0, 0, DateTimeKind.Utc);
        var checkIn = new DateTime(2024, 3, 3, 12, 0, 0, DateTimeKind.Utc);
        var masterAccepted = new DateTime(2024, 3, 4, 13, 0, 0, DateTimeKind.Utc);
        var masterDeclined = new DateTime(2024, 3, 5, 14, 0, 0, DateTimeKind.Utc);
        var playerDeclined = new DateTime(2024, 3, 6, 15, 0, 0, DateTimeKind.Utc);

        var row = MakeRow(claims:
        [
            MakeClaimRow(
                claimId: 55,
                playerUserId: 1,
                claimStatus: ClaimStatus.DeclinedByMaster,
                claimDenialStatus: ClaimDenialReason.NotSuitable,
                responsibleMasterUserId: 2,
                createDate: createDate,
                lastUpdateDateTime: lastUpdate,
                masterAcceptedDate: masterAccepted,
                masterDeclinedDate: masterDeclined,
                playerDeclinedDate: playerDeclined,
                checkInDate: checkIn),
        ]);

        var result = CharacterInfoMapper.Map(row, ProjectInfo);
        var claim = result.Claims.Single();

        claim.ClaimId.ShouldBe(new ClaimIdentification(ProjectId, 55));
        claim.PlayerId.ShouldBe(new UserIdentification(1));
        claim.Status.ShouldBe(ClaimStatus.DeclinedByMaster);
        claim.DenialStatus.ShouldBe(ClaimDenialReason.NotSuitable);
        claim.ResponsibleMasterId.ShouldBe(new UserIdentification(2));
        claim.CreateDate.ShouldBe(createDate);
        claim.LastUpdateDateTime.ShouldBe(lastUpdate);
        claim.CheckInDate.ShouldBe(checkIn);
        claim.MasterAcceptedDate.ShouldBe(masterAccepted);
        claim.MasterDeclinedDate.ShouldBe(masterDeclined);
        claim.PlayerDeclinedDate.ShouldBe(playerDeclined);
    }

    // 9в. Игрок заявки: отображаемое имя собирается из тех же частей, что и везде в проекте.

    [Fact]
    public void Map_ClaimPlayer_ShouldPreferPrefferedName()
    {
        var row = MakeRow(claims:
        [
            MakeClaimRow(
                playerUserId: 42,
                playerPrefferedName: "Лёха",
                playerBornName: "Алексей",
                playerSurName: "Иванов",
                playerEmail: "alexey@example.com"),
        ]);

        var player = CharacterInfoMapper.Map(row, ProjectInfo).Claims.Single().Player;

        player.UserId.ShouldBe(new UserIdentification(42));
        player.DisplayName.DisplayName.ShouldBe("Лёха");
        player.DisplayName.FullName.ShouldBe("Алексей Иванов");
    }

    [Fact]
    public void Map_ClaimPlayerWithoutPrefferedName_ShouldFallBackToFullName()
    {
        var row = MakeRow(claims:
        [
            MakeClaimRow(playerBornName: "Алексей", playerSurName: "Иванов"),
        ]);

        CharacterInfoMapper.Map(row, ProjectInfo).Claims.Single()
            .Player.DisplayName.DisplayName.ShouldBe("Алексей Иванов");
    }

    [Fact]
    public void Map_ClaimPlayerWithoutAnyName_ShouldFallBackToEmailUserPart()
    {
        // Регистрация не требует имени, поэтому пустое имя — обычный случай, а не крайний.
        var row = MakeRow(claims: [MakeClaimRow(playerEmail: "alexey@example.com")]);

        CharacterInfoMapper.Map(row, ProjectInfo).Claims.Single()
            .Player.DisplayName.DisplayName.ShouldBe("alexey");
    }

    [Fact]
    public void Map_ClaimPlayer_ShouldMatchEntityDisplayName()
    {
        // Страж от перепутанных колонок: маппер должен дать ровно то же, что общая сборка имени
        // из сущности. Иначе имя персонажа, записанное по игроку, разойдётся с остальным сайтом.
        var user = _mock.Player;

        var row = MakeRow(claims:
        [
            MakeClaimRow(
                playerUserId: user.UserId,
                playerPrefferedName: user.PrefferedName,
                playerBornName: user.BornName,
                playerSurName: user.SurName,
                playerFatherName: user.FatherName,
                playerEmail: user.Email),
        ]);

        CharacterInfoMapper.Map(row, ProjectInfo).Claims.Single()
            .Player.DisplayName.ShouldBe(user.ExtractDisplayName());
    }
}
