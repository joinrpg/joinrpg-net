using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.ProjectMetadata;
using static JoinRpg.DomainTypes.Test.ProjectInfoFixture;

namespace JoinRpg.DomainTypes.Test.Characters;

public class CharacterInfoTest
{
    private static readonly UserIdentification PlayerId = new(200);

    #region Инварианты конструктора

    [Fact]
    public void ShouldRejectCharacterFromAnotherProject()
    {
        var projectInfo = MakeProject(MakeField(1));

        var ex = Should.Throw<ArgumentException>(() => MakeCharacter(
            projectInfo,
            id: new CharacterIdentification(new ProjectIdentification(2), 1)));

        ex.ParamName.ShouldBe("id");
    }

    [Fact]
    public void ShouldRejectCharacterFieldLayerFromAnotherProjectInfoInstance()
    {
        var projectInfo = MakeProject(MakeField(1));
        var otherProjectInfo = MakeProject(MakeField(1));

        var ex = Should.Throw<ArgumentException>(() => MakeCharacter(
            projectInfo,
            characterFields: new FieldLayerContainer(otherProjectInfo, new Dictionary<int, string?>())));

        ex.ParamName.ShouldBe("characterFields");
    }

    [Fact]
    public void ShouldRejectClaimFromAnotherProject()
    {
        var projectInfo = MakeProject(MakeField(1));
        var alienClaim = MakeClaim(projectInfo, 1) with
        {
            ClaimId = new ClaimIdentification(new ProjectIdentification(2), 1),
        };

        var ex = Should.Throw<ArgumentException>(() => MakeCharacter(projectInfo, claims: [alienClaim]));

        ex.ParamName.ShouldBe("claims");
    }

    [Fact]
    public void ShouldRejectClaimFieldLayerFromAnotherProjectInfoInstance()
    {
        var projectInfo = MakeProject(MakeField(1));
        var otherProjectInfo = MakeProject(MakeField(1));
        var claim = MakeClaim(projectInfo, 1) with
        {
            Fields = new FieldLayerContainer(otherProjectInfo, new Dictionary<int, string?>()),
        };

        var ex = Should.Throw<ArgumentException>(() => MakeCharacter(projectInfo, claims: [claim]));

        ex.ParamName.ShouldBe("claims");
    }

    [Fact]
    public void ShouldRejectTwoApprovedClaims()
    {
        var projectInfo = MakeProject(MakeField(1));
        var first = MakeClaim(projectInfo, 1, ClaimStatus.Approved);
        var second = MakeClaim(projectInfo, 2, ClaimStatus.CheckedIn);

        var ex = Should.Throw<ArgumentException>(() => MakeCharacter(projectInfo, claims: [first, second]));

        ex.ParamName.ShouldBe("claims");
    }

    [Fact]
    public void ShouldRejectApprovedClaimIdThatIsNotAmongClaims()
    {
        var projectInfo = MakeProject(MakeField(1));
        var claim = MakeClaim(projectInfo, 1, ClaimStatus.Approved);

        var ex = Should.Throw<ArgumentException>(() => MakeCharacter(
            projectInfo,
            claims: [claim],
            approvedClaimId: new ClaimIdentification(ProjectId, 999)));

        ex.ParamName.ShouldBe("approvedClaimId");
    }

    [Fact]
    public void ShouldRejectApprovedClaimIdPointingToNotApprovedClaim()
    {
        var projectInfo = MakeProject(MakeField(1));
        var claim = MakeClaim(projectInfo, 1, ClaimStatus.Discussed);

        var ex = Should.Throw<ArgumentException>(() => MakeCharacter(
            projectInfo,
            claims: [claim],
            approvedClaimId: claim.ClaimId));

        ex.ParamName.ShouldBe("approvedClaimId");
    }

    #endregion

    #region Заявки

    [Fact]
    public void ApprovedClaimShouldBeSameInstanceAsInClaims()
    {
        var projectInfo = MakeProject(MakeField(1));
        var approved = MakeClaim(projectInfo, 1, ClaimStatus.Approved);
        var other = MakeClaim(projectInfo, 2, ClaimStatus.Discussed);

        var character = MakeCharacter(projectInfo, claims: [other, approved], approvedClaimId: approved.ClaimId);

        character.ApprovedClaim.ShouldBeSameAs(approved);
    }

    [Fact]
    public void ApprovedClaimShouldBeNullWhenNoApprovedClaimId()
    {
        var projectInfo = MakeProject(MakeField(1));

        var character = MakeCharacter(projectInfo, claims: [MakeClaim(projectInfo, 1, ClaimStatus.Discussed)]);

        character.ApprovedClaim.ShouldBeNull();
    }

    [Theory]
    [InlineData(ClaimStatus.AddedByUser, true)]
    [InlineData(ClaimStatus.AddedByMaster, true)]
    [InlineData(ClaimStatus.Discussed, true)]
    [InlineData(ClaimStatus.Approved, true)]
    [InlineData(ClaimStatus.CheckedIn, true)]
    [InlineData(ClaimStatus.OnHold, false)]
    [InlineData(ClaimStatus.DeclinedByUser, false)]
    [InlineData(ClaimStatus.DeclinedByMaster, false)]
    public void ActiveClaimsShouldFollowClaimStatus(ClaimStatus status, bool expectedActive)
    {
        var projectInfo = MakeProject(MakeField(1));

        var character = MakeCharacter(projectInfo, claims: [MakeClaim(projectInfo, 1, status)]);

        character.ActiveClaimsCount.ShouldBe(expectedActive ? 1 : 0);
        character.HasActiveClaims.ShouldBe(expectedActive);
        character.ActiveClaims.Count().ShouldBe(expectedActive ? 1 : 0);
    }

    [Fact]
    public void GetClaimByIdShouldThrowForForeignClaim()
    {
        var projectInfo = MakeProject(MakeField(1));
        var character = MakeCharacter(projectInfo, claims: [MakeClaim(projectInfo, 1)]);

        Should.Throw<KeyNotFoundException>(() => character.GetClaimById(new ClaimIdentification(ProjectId, 42)));
    }

    #endregion

    #region Группы

    [Fact]
    public void ParentGroupIdsToTopShouldClimbToRoot()
    {
        // 1 (корень) <- 2 <- 3, персонаж лежит в 3
        var projectInfo = Build(groups: MakeGroupTree(new Dictionary<int, int[]>
        {
            [2] = [1],
            [3] = [2],
        }));

        var character = MakeCharacter(projectInfo, directGroupIds: [GroupId(3)]);

        character.ParentGroupIdsToTop.ShouldBe([GroupId(3), GroupId(2), GroupId(1)], ignoreOrder: true);
    }

    [Fact]
    public void ParentGroupIdsToTopShouldDeduplicateDiamond()
    {
        // 1 (корень) <- 2, 1 <- 3, обе -> 4; персонаж в 4
        var projectInfo = Build(groups: MakeGroupTree(new Dictionary<int, int[]>
        {
            [2] = [1],
            [3] = [1],
            [4] = [2, 3],
        }));

        var character = MakeCharacter(projectInfo, directGroupIds: [GroupId(4)]);

        character.ParentGroupIdsToTop.Count.ShouldBe(4);
        character.ParentGroupIdsToTop.Count(g => g == GroupId(1)).ShouldBe(1);
    }

    [Fact]
    public void ParentGroupIdsToTopShouldBeEmptyForCharacterWithoutGroups()
    {
        var projectInfo = Build(groups: MakeGroupTree(new Dictionary<int, int[]>()));

        var character = MakeCharacter(projectInfo, directGroupIds: []);

        character.ParentGroupIdsToTop.ShouldBeEmpty();
    }

    [Fact]
    public void IntrestingGroupsForDisplayShouldSkipRootSpecialAndInactive()
    {
        var projectInfo = Build(groups: MakeGroupTree(
            new Dictionary<int, int[]>
            {
                [2] = [1],
                [3] = [1],
                [4] = [1],
                [5] = [1],
            },
            types: new Dictionary<int, CharacterGroupType>
            {
                [3] = CharacterGroupType.SpecialToField,
                [4] = CharacterGroupType.SpecialToValue,
            },
            isActiveByGroup: new Dictionary<int, bool> { [5] = false }));

        var character = MakeCharacter(projectInfo, directGroupIds: [GroupId(2), GroupId(3), GroupId(4), GroupId(5)]);

        // 2 — обычная, 4 — SpecialToValue (тоже интересная); 1 корень, 3 спец-по-полю, 5 неактивна
        character.IntrestingGroupsForDisplay.Select(g => g.Id).ShouldBe([GroupId(2), GroupId(4)], ignoreOrder: true);
    }

    #endregion

    #region Ответственный мастер

    [Fact]
    public void ResponsibleMasterShouldComeFromApprovedClaim()
    {
        var projectInfo = MakeProject(MakeField(1));
        var claimMaster = new UserIdentification(555);
        var approved = MakeClaim(projectInfo, 1, ClaimStatus.Approved, responsibleMasterId: claimMaster);

        var character = MakeCharacter(projectInfo, claims: [approved], approvedClaimId: approved.ClaimId);

        character.ResponsibleMasterId.ShouldBe(claimMaster);
    }

    [Fact]
    public void ResponsibleMasterShouldComeFromGroupRuleWhenNoApprovedClaim()
    {
        var groupMaster = new UserIdentification(777);
        var groups = MakeGroupTree(
            new Dictionary<int, int[]> { [2] = [1] },
            responsibleMasterByGroup: new Dictionary<int, UserIdentification> { [2] = groupMaster });

        var projectInfo = Build(
            groups: groups,
            masters: [MakeMaster(DefaultMasterId, isOwner: true), MakeMaster(groupMaster)],
            responsibleMasterRules: [groups[GroupId(2)]]);

        var character = MakeCharacter(projectInfo, directGroupIds: [GroupId(2)]);

        character.ResponsibleMasterId.ShouldBe(groupMaster);
    }

    [Fact]
    public void ResponsibleMasterShouldFallBackToProjectOwner()
    {
        var projectInfo = Build(groups: MakeGroupTree(new Dictionary<int, int[]> { [2] = [1] }));

        var character = MakeCharacter(projectInfo, directGroupIds: [GroupId(2)]);

        character.ResponsibleMasterId.ShouldBe(DefaultMasterId);
    }

    #endregion

    #region GetFieldLayers

    [Fact]
    public void ApprovedClaimLayerShouldOverrideCharacterLayer()
    {
        var projectInfo = MakeProject(MakeField(1, boundTo: FieldBoundTo.Character));
        var approved = MakeClaim(projectInfo, 1, ClaimStatus.Approved, fields: new() { { 1, "from claim" } });
        var character = MakeCharacter(
            projectInfo,
            characterFields: new FieldLayerContainer(projectInfo, new Dictionary<int, string?> { { 1, "from character" } }),
            claims: [approved],
            approvedClaimId: approved.ClaimId);

        var layers = character.GetFieldLayers(AccessArgumentsMaster);

        layers.GetFieldValue(new ProjectFieldIdentification(ProjectId, 1))!.Value.ShouldBe("from claim");
        layers.CharacterLayer.ShouldBeSameAs(character.CharacterFields);
    }

    [Fact]
    public void WithoutApprovedClaimThereIsNoClaimLayer()
    {
        var projectInfo = MakeProject(MakeField(1, boundTo: FieldBoundTo.Claim));
        var character = MakeCharacter(projectInfo, claims: [MakeClaim(projectInfo, 1, ClaimStatus.Discussed)]);

        var layers = character.GetFieldLayers(AccessArgumentsMaster);

        layers.ClaimLayer.ShouldBeNull();
        layers.GetFieldValue(new ProjectFieldIdentification(ProjectId, 1)).ShouldBeNull();
    }

    [Fact]
    public void ExplicitClaimIdShouldSelectThatClaimLayer()
    {
        var projectInfo = MakeProject(MakeField(1, boundTo: FieldBoundTo.Character));
        var approved = MakeClaim(projectInfo, 1, ClaimStatus.Approved, fields: new() { { 1, "approved" } });
        var discussed = MakeClaim(projectInfo, 2, ClaimStatus.Discussed, fields: new() { { 1, "discussed" } });
        var character = MakeCharacter(
            projectInfo,
            claims: [approved, discussed],
            approvedClaimId: approved.ClaimId);

        var layers = character.GetFieldLayers(AccessArgumentsMaster, discussed.ClaimId);

        layers.GetFieldValue(new ProjectFieldIdentification(ProjectId, 1))!.Value.ShouldBe("discussed");
    }

    [Fact]
    public void GetFieldLayersShouldThrowForForeignClaim()
    {
        var projectInfo = MakeProject(MakeField(1));
        var character = MakeCharacter(projectInfo, claims: [MakeClaim(projectInfo, 1)]);

        Should.Throw<KeyNotFoundException>(
            () => character.GetFieldLayers(AccessArgumentsMaster, new ClaimIdentification(ProjectId, 42)));
    }

    [Fact]
    public void MasterOnlyFieldShouldBeHiddenFromPlayer()
    {
        var projectInfo = MakeProject(
            MakeField(1, visibility: ProjectFieldVisibility.Public),
            MakeField(2, visibility: ProjectFieldVisibility.MasterOnly));
        var character = MakeCharacter(
            projectInfo,
            characterFields: new FieldLayerContainer(
                projectInfo,
                new Dictionary<int, string?> { { 1, "public" }, { 2, "secret" } }));

        var masterView = character.GetFieldLayers(AccessArgumentsMaster).GetSortedFieldsForView();
        var playerView = character.GetFieldLayers(AccessArgumentsPlayer).GetSortedFieldsForView();

        masterView.Select(f => f.Value).ShouldBe(["public", "secret"], ignoreOrder: true);
        playerView.Select(f => f.Value).ShouldBe(["public"]);
    }

    #endregion

    #region Достаточность для потребителей

    /// <summary>
    /// Страж: тройка входов <c>BusyStatusExtensions.GetBusyStatus</c> целиком читается из
    /// <see cref="CharacterInfo"/> — отдельный запрос к БД для сетки ролей не нужен (ADR013).
    /// </summary>
    [Theory]
    [InlineData(CharacterType.Player, false, false)]
    [InlineData(CharacterType.Player, true, false)]
    [InlineData(CharacterType.NonPlayer, false, false)]
    [InlineData(CharacterType.Slot, false, true)]
    public void BusyStatusInputsShouldBeAvailable(CharacterType type, bool hasApprovedClaim, bool hasActiveClaim)
    {
        var projectInfo = MakeProject(MakeField(1));
        var typeInfo = type == CharacterType.Slot
            ? CharacterTypeInfo.DefaultSlot("slot")
            : new CharacterTypeInfo(type, false, null, null, CharacterVisibility.Public);

        List<CharacterClaimInfo> claims = [];
        ClaimIdentification? approvedClaimId = null;
        if (hasApprovedClaim)
        {
            var approved = MakeClaim(projectInfo, 1, ClaimStatus.Approved);
            claims.Add(approved);
            approvedClaimId = approved.ClaimId;
        }
        if (hasActiveClaim)
        {
            claims.Add(MakeClaim(projectInfo, 2, ClaimStatus.Discussed));
        }

        var character = MakeCharacter(projectInfo, characterTypeInfo: typeInfo, claims: claims, approvedClaimId: approvedClaimId);

        character.CharacterTypeInfo.ShouldBe(typeInfo);
        (character.ApprovedClaimId is not null).ShouldBe(hasApprovedClaim);
        character.HasActiveClaims.ShouldBe(hasApprovedClaim || hasActiveClaim);
    }

    /// <summary>
    /// Страж: заявка конкретного игрока (вход <c>AddClaimForbideReason.AlreadySent</c>) находится
    /// без обращения к БД.
    /// </summary>
    [Fact]
    public void ActiveClaimOfPlayerShouldBeFindable()
    {
        var projectInfo = MakeProject(MakeField(1));
        var otherPlayer = new UserIdentification(300);
        var character = MakeCharacter(projectInfo, claims:
        [
            MakeClaim(projectInfo, 1, ClaimStatus.Discussed),
            MakeClaim(projectInfo, 2, ClaimStatus.DeclinedByUser, playerId: otherPlayer),
        ]);

        character.Claims.Any(c => c.PlayerId == PlayerId && c.IsActive).ShouldBeTrue();
        character.Claims.Any(c => c.PlayerId == otherPlayer && c.IsActive).ShouldBeFalse();
    }

    #endregion

    #region Витеры и ForNewCharacter (нужны на пути записи)

    [Fact]
    public void WithDirectGroupsShouldReplaceGroupsAndKeepEverythingElse()
    {
        var projectInfo = Build(groups: MakeGroupTree(new Dictionary<int, int[]> { [2] = [1] }));
        var claim = MakeClaim(projectInfo, 1, ClaimStatus.Approved);
        var character = MakeCharacter(
            projectInfo, directGroupIds: [], claims: [claim], approvedClaimId: claim.ClaimId);

        var updated = character.WithDirectGroups([GroupId(2)]);

        updated.DirectGroupIds.ShouldBe([GroupId(2)]);
        updated.ParentGroupIdsToTop.ShouldBe([GroupId(2), GroupId(1)], ignoreOrder: true);
        updated.Id.ShouldBe(character.Id);
        updated.ApprovedClaim.ShouldBeSameAs(claim);
        updated.CharacterFields.ShouldBeSameAs(character.CharacterFields);
        // Исходный агрегат не изменился.
        character.DirectGroupIds.ShouldBeEmpty();
    }

    [Fact]
    public void WithCharacterTypeInfoShouldReplaceTypeAndKeepEverythingElse()
    {
        var projectInfo = MakeProject(MakeField(1));
        var character = MakeCharacter(projectInfo);

        var updated = character.WithCharacterTypeInfo(
            new CharacterTypeInfo(CharacterType.NonPlayer, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Private));

        updated.CharacterType.ShouldBe(CharacterType.NonPlayer);
        updated.IsPublic.ShouldBeFalse();
        updated.CharacterName.ShouldBe(character.CharacterName);
    }

    [Fact]
    public void WithersShouldKeepConstructorInvariants()
    {
        // Витер прогоняет основной конструктор, поэтому проверки остаются в одном месте.
        var projectInfo = Build(groups: MakeGroupTree(new Dictionary<int, int[]>()));
        var character = MakeCharacter(projectInfo);

        _ = Should.Throw<ArgumentException>(
            () => character.WithCharacterTypeInfo(
                new CharacterTypeInfo(CharacterType.NonPlayer, IsHot: true, SlotLimit: null, SlotName: null, CharacterVisibility.Public)));
    }

    [Fact]
    public void ForNewCharacterShouldHaveNoClaims()
    {
        var projectInfo = Build(
            fields: [MakeField(1)],
            groups: MakeGroupTree(new Dictionary<int, int[]> { [2] = [1] }));
        var createdAt = new DateTime(2026, 8, 30);

        // Персонажа ещё нет в БД, поэтому id отрицательный — ровно то, что видит сохранение полей.
        var character = CharacterInfo.ForNewCharacter(
            new CharacterIdentification(ProjectId, -1),
            projectInfo,
            "Новый",
            CharacterTypeInfo.Default(),
            hidePlayerForCharacter: false,
            [GroupId(2)],
            new FieldLayerContainer(projectInfo, new Dictionary<int, string?> { { 1, "x" } }),
            DefaultMasterId,
            createdAt);

        character.Claims.ShouldBeEmpty();
        character.ApprovedClaim.ShouldBeNull();
        character.ApprovedClaimId.ShouldBeNull();
        character.HasActiveClaims.ShouldBeFalse();
        character.IsActive.ShouldBeTrue();
        character.Id.CharacterId.ShouldBe(-1);
        character.ParentGroupIdsToTop.ShouldBe([GroupId(2), GroupId(1)], ignoreOrder: true);
        character.CreatedAt.ShouldBe(createdAt);
        character.UpdatedAt.ShouldBe(createdAt);
    }

    [Fact]
    public void ForNewCharacterResponsibleMasterShouldComeFromGroups()
    {
        // Утверждённой заявки нет, поэтому ответственный выбирается по правилам групп.
        var projectInfo = Build(groups: MakeGroupTree(new Dictionary<int, int[]>()));

        var character = CharacterInfo.ForNewCharacter(
            new CharacterIdentification(ProjectId, -1),
            projectInfo,
            "Новый",
            CharacterTypeInfo.Default(),
            hidePlayerForCharacter: false,
            [],
            new FieldLayerContainer(projectInfo, new Dictionary<int, string?>()),
            DefaultMasterId,
            new DateTime(2026, 8, 30));

        character.ResponsibleMasterId.ShouldBe(DefaultMasterId);
    }

    #endregion

    #region GetFieldLayers по объекту заявки

    [Fact]
    public void GetFieldLayersShouldAcceptClaimThatIsNotAmongClaims()
    {
        // Так сохраняются поля ещё не созданной заявки: её нет ни в БД, ни в агрегате.
        var projectInfo = MakeProject(MakeField(1, boundTo: FieldBoundTo.Claim));
        var character = MakeCharacter(projectInfo);
        var unsavedClaim = MakeClaim(projectInfo, -1, fields: new() { { 1, "из новой заявки" } });

        var layers = character.GetFieldLayers(AccessArgumentsMaster, unsavedClaim);

        layers.GetFieldValue(new ProjectFieldIdentification(ProjectId, 1))!.Value.ShouldBe("из новой заявки");
    }

    [Fact]
    public void GetFieldLayersWithoutClaimShouldGiveOnlyCharacterLayer()
    {
        var projectInfo = MakeProject(MakeField(1));
        var character = MakeCharacter(projectInfo);

        var layers = character.GetFieldLayers(AccessArgumentsMaster, claim: null);

        layers.ClaimLayer.ShouldBeNull();
        layers.CharacterLayer.ShouldBeSameAs(character.CharacterFields);
    }

    [Fact]
    public void GetFieldLayersShouldNotFilterCharacterLayerByAccess()
    {
        // Поверх этих слоёв считаются взносы (FinanceExtensions), поэтому фильтровать слой
        // персонажа по правам здесь нельзя — иначе непубличные платные поля пропадут из расчёта.
        var projectInfo = MakeProject(MakeField(1, visibility: ProjectFieldVisibility.MasterOnly));
        var character = MakeCharacter(
            projectInfo,
            characterFields: new FieldLayerContainer(projectInfo, new Dictionary<int, string?> { { 1, "секрет" } }));

        var layers = character.GetFieldLayers(AccessArgumentsNone, claim: null);

        layers.CharacterLayer.LayerData.Count.ShouldBe(1);
    }

    #endregion

    private static CharacterInfo MakeCharacter(
        ProjectInfo projectInfo,
        CharacterIdentification? id = null,
        CharacterTypeInfo? characterTypeInfo = null,
        FieldLayerContainer? characterFields = null,
        IReadOnlyCollection<CharacterGroupIdentification>? directGroupIds = null,
        IReadOnlyCollection<CharacterClaimInfo>? claims = null,
        ClaimIdentification? approvedClaimId = null)
        => new(
            id ?? new CharacterIdentification(ProjectId, 1),
            projectInfo,
            "Вася",
            characterTypeInfo ?? CharacterTypeInfo.Default(),
            hidePlayerForCharacter: false,
            isActive: true,
            inGame: false,
            autoCreated: false,
            new MarkdownString(""),
            originalCharacterSlotId: null,
            directGroupIds ?? [],
            characterFields ?? new FieldLayerContainer(projectInfo, new Dictionary<int, string?>()),
            claims ?? [],
            approvedClaimId,
            new DateTime(2024, 1, 1),
            DefaultMasterId,
            new DateTime(2024, 1, 2),
            DefaultMasterId);

    private static UserInfoHeader MakePlayer(UserIdentification userId)
        => new(userId, new UserDisplayName($"Игрок{userId.Value}", null));

    private static CharacterClaimInfo MakeClaim(
        ProjectInfo projectInfo,
        int claimId,
        ClaimStatus status = ClaimStatus.AddedByUser,
        UserIdentification? playerId = null,
        UserIdentification? responsibleMasterId = null,
        Dictionary<int, string?>? fields = null)
        => new(
            new ClaimIdentification(ProjectId, claimId),
            MakePlayer(playerId ?? PlayerId),
            status,
            DenialStatus: null,
            responsibleMasterId ?? DefaultMasterId,
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 2),
            CheckInDate: null,
            LastPlayerCommentAt: null,
            LastMasterCommentAt: null,
            LastVisibleMasterCommentAt: null,
            CurrentFee: null,
            PreferentialFeeUser: false,
            FeePaid: 0,
            AccommodationFee: 0,
            new FieldLayerContainer(projectInfo, fields ?? []));
}
