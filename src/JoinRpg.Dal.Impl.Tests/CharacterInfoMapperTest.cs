using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Extensions;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.ProjectMetadata;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Tests;

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
        ClaimStatus claimStatus = ClaimStatus.AddedByUser,
        ClaimDenialReason? claimDenialStatus = null,
        int responsibleMasterUserId = 2,
        DateTime? createDate = null,
        DateTime? lastUpdateDateTime = null,
        DateTime? checkInDate = null,
        DateTimeOffset? lastPlayerCommentAt = null,
        DateTimeOffset? lastMasterCommentAt = null,
        DateTimeOffset? lastVisibleMasterCommentAt = null,
        int? currentFee = null,
        bool preferentialFeeUser = false,
        string? jsonData = null,
        int? feePaid = null,
        int? accommodationFee = null)
        => new()
        {
            ClaimId = claimId,
            PlayerUserId = playerUserId,
            ClaimStatus = claimStatus,
            ClaimDenialStatus = claimDenialStatus,
            ResponsibleMasterUserId = responsibleMasterUserId,
            CreateDate = createDate ?? SomeDate,
            LastUpdateDateTime = lastUpdateDateTime ?? SomeDate,
            CheckInDate = checkInDate,
            LastPlayerCommentAt = lastPlayerCommentAt,
            LastMasterCommentAt = lastMasterCommentAt,
            LastVisibleMasterCommentAt = lastVisibleMasterCommentAt,
            CurrentFee = currentFee,
            PreferentialFeeUser = preferentialFeeUser,
            JsonData = jsonData,
            FeePaid = feePaid,
            AccommodationFee = accommodationFee,
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

    // 4. FeePaid == null -> 0, AccommodationFee == null -> 0; ненулевые значения проходят как есть.

    [Fact]
    public void Map_ClaimFeePaidAndAccommodationFeeNull_ShouldMapToZero()
    {
        var row = MakeRow(claims: [MakeClaimRow(feePaid: null, accommodationFee: null)]);

        var result = CharacterInfoMapper.Map(row, ProjectInfo);

        var claim = result.Claims.Single();
        claim.FeePaid.ShouldBe(0);
        claim.AccommodationFee.ShouldBe(0);
    }

    [Fact]
    public void Map_ClaimFeePaidAndAccommodationFeeSet_ShouldPassThroughAsIs()
    {
        var row = MakeRow(claims: [MakeClaimRow(feePaid: 1500, accommodationFee: 300)]);

        var result = CharacterInfoMapper.Map(row, ProjectInfo);

        var claim = result.Claims.Single();
        claim.FeePaid.ShouldBe(1500);
        claim.AccommodationFee.ShouldBe(300);
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
    }
}
