using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Impl.Claims;
using JoinRpg.Services.Interfaces.Drp;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Services.Impl.Test.Claims;

/// <summary>
/// Восстановление полей, групп персонажей и полей заявок из выгрузки мастера (DRP, #5529): только в
/// заблокированном проекте, в режиме восстановления полей, без уведомлений.
/// </summary>
public class ExportRestoreServiceTest : ClaimServiceTestBase
{
    private ExportRestoreService CreateService(int? currentUserId = null)
        => new(
            CreatePropsService(currentUserId),
            unitOfWork,
            metadataRepository,
            CreateCurrentUser(currentUserId),
            CreateFieldSetupService(currentUserId),
            CreateUserFieldValidator(),
            NullLogger<ExportRestoreService>.Instance);

    private void BlockProject()
    {
        mock.Project.IsBlocked = true;
        mock.ReInitProjectInfo();
    }

    /// <summary>Строковое поле персонажа; уже использованное — чтобы отметка не давала лишнего сохранения.</summary>
    private ProjectFieldInfo AddCharacterField(
        string name,
        MandatoryStatus mandatoryStatus = MandatoryStatus.Optional,
        FieldBoundTo boundTo = FieldBoundTo.Character,
        bool wasEverUsed = true,
        bool isPublic = false)
        => mock.AddField(f =>
        {
            f.FieldName = name;
            f.FieldType = ProjectFieldType.String;
            f.FieldBoundTo = boundTo;
            f.MandatoryStatus = mandatoryStatus;
            f.IsPublic = isPublic;
            f.CanPlayerView = true;
            f.CanPlayerEdit = true;
            f.ShowOnUnApprovedClaims = true;
            f.ValidForNpc = true;
            f.ValuesOrdering = "";
            f.WasEverUsed = wasEverUsed;
        });

    private string? GetCharacterValue(Character character, ProjectFieldInfo field)
        => FieldLayerContainer.DeserializeFieldLayer(mock.ProjectInfo, character.JsonData).GetValue(field);

    private string? GetClaimValue(Claim claim, ProjectFieldInfo field)
        => FieldLayerContainer.DeserializeFieldLayer(mock.ProjectInfo, claim.JsonData).GetValue(field);

    private static Dictionary<ProjectFieldIdentification, string?> Fields(params (ProjectFieldInfo Field, string? Value)[] values)
        => values.ToDictionary(v => v.Field.Id, v => v.Value);

    private static List<Comment> RestoreComments(Claim claim)
        => [.. claim.CommentDiscussion.Comments.Where(c => c.ExtraAction == CommentExtraAction.RestoredFromExport)];

    #region ApplyRestoredCharacter

    [Fact]
    public async Task Character_ActiveProject_Throws_AndNothingIsSaved()
    {
        var field = AddCharacterField("Поле", wasEverUsed: false);

        _ = await Should.ThrowAsync<JoinValidationException>(
            () => CreateService().ApplyRestoredCharacter(
                mock.Character.GetId(), new RestoredCharacterTarget(null, Fields((field, "значение")))));

        GetCharacterValue(mock.Character, field).ShouldBeNull();
        mock.Project.ProjectFields.Single(f => f.ProjectFieldId == field.Id.ProjectFieldId).WasEverUsed.ShouldBeFalse();
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Character_ByPlayer_Throws_AndNothingIsSaved()
    {
        var field = AddCharacterField("Поле", wasEverUsed: false);
        BlockProject();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).ApplyRestoredCharacter(
                mock.Character.GetId(), new RestoredCharacterTarget(null, Fields((field, "значение")))));

        GetCharacterValue(mock.Character, field).ShouldBeNull();
        mock.Project.ProjectFields.Single(f => f.ProjectFieldId == field.Id.ProjectFieldId).WasEverUsed.ShouldBeFalse();
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Character_WritesGivenFields_KeepsOthers_AndAllowsEmptyMandatory()
    {
        var restored = AddCharacterField("Восстановленное");
        var mandatory = AddCharacterField("Обязательное", MandatoryStatus.Required);
        var untouched = AddCharacterField("Не из выгрузки");
        MockedProject.AssignFieldValues(mock.Character, new FieldWithValue(untouched, "осталось"));
        BlockProject();

        await CreateService().ApplyRestoredCharacter(
            mock.Character.GetId(),
            new RestoredCharacterTarget(null, Fields((restored, "из выгрузки"), (mandatory, null))));

        GetCharacterValue(mock.Character, restored).ShouldBe("из выгрузки");
        GetCharacterValue(mock.Character, mandatory).ShouldBeNull();
        GetCharacterValue(mock.Character, untouched).ShouldBe("осталось");
        SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Character_SetsRegularGroups_AndRecomputesSpecialGroupsFromFields()
    {
        var specialGroup = mock.CreateSpecialGroup();
        var (dropdown, variant) = mock.AddDropdownFieldWithSpecialGroup("Выбор", specialGroup);
        BlockProject();

        await CreateService().ApplyRestoredCharacter(
            mock.Character.GetId(),
            new RestoredCharacterTarget(
                [new CharacterGroupIdentification(ProjectId, mock.Group.CharacterGroupId)],
                Fields((dropdown, variant.Id.ProjectFieldVariantId.ToString()))));

        mock.Character.ParentCharacterGroupIds.ShouldBe(
            [mock.Group.CharacterGroupId, specialGroup.CharacterGroupId], ignoreOrder: true);
    }

    [Fact]
    public async Task Character_NullGroups_KeepsRegularGroups()
    {
        var field = AddCharacterField("Поле");
        MockedProject.AddCharToGroup(mock.Character, mock.Group);
        var groupsBefore = mock.Character.ParentCharacterGroupIds.ToArray();
        BlockProject();

        await CreateService().ApplyRestoredCharacter(
            mock.Character.GetId(), new RestoredCharacterTarget(null, Fields((field, "значение"))));

        mock.Character.ParentCharacterGroupIds.ShouldBe(groupsBefore, ignoreOrder: true);
    }

    [Fact]
    public async Task Character_GroupsOnly_ChangesGroups()
    {
        BlockProject();

        await CreateService().ApplyRestoredCharacter(
            mock.Character.GetId(),
            new RestoredCharacterTarget([new CharacterGroupIdentification(ProjectId, mock.Group.CharacterGroupId)], null));

        mock.Character.ParentCharacterGroupIds.ShouldBe([mock.Group.CharacterGroupId]);
        SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Character_SameState_SavesNothing()
    {
        var field = AddCharacterField("Поле");
        MockedProject.AssignFieldValues(mock.Character, new FieldWithValue(field, "значение"));
        var groups = mock.Character.ParentCharacterGroupIds
            .Select(id => new CharacterGroupIdentification(ProjectId, id)).ToList();
        BlockProject();

        await CreateService().ApplyRestoredCharacter(
            mock.Character.GetId(), new RestoredCharacterTarget(groups, Fields((field, "значение"))));

        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Character_BumpsUpdatedAtToNow_AndSendsNothing()
    {
        var field = AddCharacterField("Поле");
        mock.Character.UpdatedAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        BlockProject();
        var before = DateTime.UtcNow;

        await CreateService().ApplyRestoredCharacter(
            mock.Character.GetId(), new RestoredCharacterTarget(null, Fields((field, "значение"))));

        mock.Character.UpdatedAt.ShouldBeGreaterThanOrEqualTo(before);
        SentInOrder.ShouldBeEmpty();
    }

    [Fact]
    public async Task Character_MarksFieldUsed()
    {
        var field = AddCharacterField("Поле", wasEverUsed: false);
        BlockProject();

        await CreateService().ApplyRestoredCharacter(
            mock.Character.GetId(), new RestoredCharacterTarget(null, Fields((field, "значение"))));

        mock.Project.ProjectFields.Single(f => f.ProjectFieldId == field.Id.ProjectFieldId).WasEverUsed.ShouldBeTrue();
        GetCharacterValue(mock.Character, field).ShouldBe("значение");
    }

    [Fact]
    public async Task Character_WithApprovedClaim_WritesCharacterField()
    {
        var field = AddCharacterField("Поле");
        var character = mock.CreateCharacter("Вася");
        _ = mock.CreateApprovedClaim(character, mock.Player);
        BlockProject();

        await CreateService().ApplyRestoredCharacter(
            character.GetId(), new RestoredCharacterTarget(null, Fields((field, "из выгрузки"))));

        GetCharacterValue(character, field).ShouldBe("из выгрузки");
        SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Character_ClaimField_Throws_AndNothingIsSaved()
    {
        var claimField = AddCharacterField("Поле заявки", boundTo: FieldBoundTo.Claim);
        BlockProject();

        _ = await Should.ThrowAsync<ArgumentException>(
            () => CreateService().ApplyRestoredCharacter(
                mock.Character.GetId(), new RestoredCharacterTarget(null, Fields((claimField, "значение")))));

        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Character_MasterWithoutCanEditRoles_Throws()
    {
        var field = AddCharacterField("Поле");
        var master = mock.CreateMaster();
        mock.Project.ProjectAcls.Single(a => a.UserId == master.UserId).CanEditRoles = false;
        BlockProject();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(master.UserId).ApplyRestoredCharacter(
                mock.Character.GetId(), new RestoredCharacterTarget(null, Fields((field, "значение")))));

        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Character_EmptyGroupList_Throws()
    {
        BlockProject();

        _ = await Should.ThrowAsync<JoinValidationException>(
            () => CreateService().ApplyRestoredCharacter(mock.Character.GetId(), new RestoredCharacterTarget([], null)));

        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Character_SpecialGroupInList_Throws()
    {
        var specialGroup = mock.CreateSpecialGroup();
        BlockProject();

        _ = await Should.ThrowAsync<SpecialCharacterGroupNotAllowedException>(
            () => CreateService().ApplyRestoredCharacter(
                mock.Character.GetId(),
                new RestoredCharacterTarget([new CharacterGroupIdentification(ProjectId, specialGroup.CharacterGroupId)], null)));

        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Character_GroupOfOtherProject_Throws()
    {
        BlockProject();
        var foreignGroup = new CharacterGroupIdentification(new ProjectIdentification(ProjectId.Value + 1), mock.Group.CharacterGroupId);

        _ = await Should.ThrowAsync<ArgumentException>(
            () => CreateService().ApplyRestoredCharacter(mock.Character.GetId(), new RestoredCharacterTarget([foreignGroup], null)));

        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Character_UnknownGroup_Throws()
    {
        BlockProject();

        _ = await Should.ThrowAsync<CharacterGroupsNotFoundException>(
            () => CreateService().ApplyRestoredCharacter(
                mock.Character.GetId(),
                new RestoredCharacterTarget([new CharacterGroupIdentification(ProjectId, 99999)], null)));

        SaveChangesCallCount.ShouldBe(0);
    }

    #endregion

    #region ApplyRestoredClaimFields

    [Fact]
    public async Task Claim_MasterWithoutCanManageClaims_Throws()
    {
        var field = AddCharacterField("Поле заявки", boundTo: FieldBoundTo.Claim);
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        var master = mock.CreateMaster();
        mock.Project.ProjectAcls.Single(a => a.UserId == master.UserId).CanManageClaims = false;
        BlockProject();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(master.UserId).ApplyRestoredClaimFields(claim.GetId(), Fields((field, "значение"))));

        GetClaimValue(claim, field).ShouldBeNull();
        SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Непубличное значение персонажа в выгрузке заявки — это то, что заявка видела через персонажа, а не
    /// предложение игрока: копировать его в заявку нельзя.
    /// </summary>
    [Fact]
    public async Task Claim_Unapproved_NonPublicCharacterValue_IsNotCopiedToClaim()
    {
        var field = AddCharacterField("Непубличное поле персонажа");
        var character = mock.CreateCharacter("Вася");
        MockedProject.AssignFieldValues(character, new FieldWithValue(field, "секрет"));
        var claim = mock.CreateClaim(character, mock.Player);
        BlockProject();

        await CreateService().ApplyRestoredClaimFields(claim.GetId(), Fields((field, "секрет")));

        GetClaimValue(claim, field).ShouldBeNull();
        RestoreComments(claim).ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Claim_Unapproved_PlayerProposal_IsWrittenToClaim_CharacterUntouched()
    {
        var field = AddCharacterField("Поле персонажа");
        var character = mock.CreateCharacter("Вася");
        MockedProject.AssignFieldValues(character, new FieldWithValue(field, "у персонажа"));
        var characterUpdatedAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        character.UpdatedAt = characterUpdatedAt;
        var claim = mock.CreateClaim(character, mock.Player);
        BlockProject();

        await CreateService().ApplyRestoredClaimFields(claim.GetId(), Fields((field, "предложение игрока")));

        GetClaimValue(claim, field).ShouldBe("предложение игрока");
        GetCharacterValue(character, field).ShouldBe("у персонажа");
        character.UpdatedAt.ShouldBe(characterUpdatedAt);
        RestoreComments(claim).ShouldHaveSingleItem();
        SaveChangesCallCount.ShouldBe(1);
    }

    /// <summary>
    /// У заявки пусто, у персонажа публичное значение, в выгрузке пусто: сохранение считает поле
    /// изменённым, но записывать нечего — ни сохранения, ни истории, сколько ни вызывай.
    /// </summary>
    [Fact]
    public async Task Claim_Unapproved_EmptyExportOverPublicCharacterValue_IsIdempotent()
    {
        var field = AddCharacterField("Публичное поле персонажа", isPublic: true);
        var character = mock.CreateCharacter("Вася");
        MockedProject.AssignFieldValues(character, new FieldWithValue(field, "публичное"));
        var claim = mock.CreateClaim(character, mock.Player);
        var claimJson = claim.JsonData;
        var characterJson = character.JsonData;
        BlockProject();

        await CreateService().ApplyRestoredClaimFields(claim.GetId(), Fields((field, null)));
        await CreateService().ApplyRestoredClaimFields(claim.GetId(), Fields((field, null)));

        claim.JsonData.ShouldBe(claimJson);
        character.JsonData.ShouldBe(characterJson);
        RestoreComments(claim).ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Claim_ActiveProject_Throws_AndNothingIsSaved()
    {
        var field = AddCharacterField("Поле заявки", boundTo: FieldBoundTo.Claim);
        var claim = mock.CreateClaim(mock.Character, mock.Player);

        _ = await Should.ThrowAsync<JoinValidationException>(
            () => CreateService().ApplyRestoredClaimFields(claim.GetId(), Fields((field, "значение"))));

        GetClaimValue(claim, field).ShouldBeNull();
        claim.CommentDiscussion.Comments.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Claim_WritesFields_WithSingleHiddenHistoryRecord_WithoutNotifications()
    {
        var field = AddCharacterField("Поле заявки", boundTo: FieldBoundTo.Claim);
        var mandatory = AddCharacterField("Обязательное", MandatoryStatus.Required, boundTo: FieldBoundTo.Claim);
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        MockedProject.AssignFieldValues(claim, new FieldWithValue(mandatory, "было"));
        claim.LastUpdateDateTime = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        BlockProject();
        var before = DateTime.UtcNow;

        await CreateService().ApplyRestoredClaimFields(
            claim.GetId(), Fields((field, "из выгрузки"), (mandatory, null)));

        GetClaimValue(claim, field).ShouldBe("из выгрузки");
        GetClaimValue(claim, mandatory).ShouldBeNull();

        var comment = claim.CommentDiscussion.Comments.ShouldHaveSingleItem();
        comment.ExtraAction.ShouldBe(CommentExtraAction.RestoredFromExport);
        comment.IsVisibleToPlayer.ShouldBeFalse();
        comment.CommentText.Text.Contents!.ShouldContain("Поле заявки");
        comment.CommentText.Text.Contents!.ShouldContain("Обязательное");
        claim.LastUpdateDateTime.ShouldBeGreaterThanOrEqualTo(before);

        SentInOrder.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Claim_SameValues_SavesNothing_AndWritesNoHistory()
    {
        var field = AddCharacterField("Поле заявки", boundTo: FieldBoundTo.Claim);
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        MockedProject.AssignFieldValues(claim, new FieldWithValue(field, "значение"));
        BlockProject();

        await CreateService().ApplyRestoredClaimFields(claim.GetId(), Fields((field, "значение")));

        RestoreComments(claim).ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Claim_Approved_WritesCharacterFieldToCharacter_AndBumpsCharacterUpdatedAt()
    {
        var field = AddCharacterField("Поле персонажа");
        var character = mock.CreateCharacter("Вася");
        var claim = mock.CreateApprovedClaim(character, mock.Player);
        character.UpdatedAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        BlockProject();
        var before = DateTime.UtcNow;

        await CreateService().ApplyRestoredClaimFields(claim.GetId(), Fields((field, "из выгрузки")));

        GetCharacterValue(character, field).ShouldBe("из выгрузки");
        character.UpdatedAt.ShouldBeGreaterThanOrEqualTo(before);
        RestoreComments(claim).ShouldHaveSingleItem();
        SentInOrder.ShouldBeEmpty();
    }

    [Fact]
    public async Task Claim_FieldFromOtherProject_Throws()
    {
        BlockProject();
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        var foreignField = new ProjectFieldIdentification(new ProjectIdentification(ProjectId.Value + 1), 1);

        _ = await Should.ThrowAsync<ArgumentException>(
            () => CreateService().ApplyRestoredClaimFields(
                claim.GetId(), new Dictionary<ProjectFieldIdentification, string?> { [foreignField] = "значение" }));

        SaveChangesCallCount.ShouldBe(0);
    }

    #endregion

    #region RestoreLostGroup

    /// <summary>Потерянный id — ниже счётчика мока, но не занятый.</summary>
    private const int LostId = 500;

    private CharacterGroupIdentification LostGroupId => new(ProjectId, LostId);

    private CharacterGroup? FindGroup(int id) => mock.Project.CharacterGroups.SingleOrDefault(g => g.CharacterGroupId == id);

    private int RootGroupId => mock.Project.CharacterGroups.Single(g => g.IsRoot).CharacterGroupId;

    [Fact]
    public async Task LostGroup_ActiveProject_Throws_AndNothingIsInserted()
    {
        _ = await Should.ThrowAsync<JoinValidationException>(() => CreateService().RestoreLostGroup(LostGroupId, "Потерянная"));

        FindGroup(LostId).ShouldBeNull();
    }

    [Fact]
    public async Task LostGroup_ByPlayer_Throws_AndNothingIsInserted()
    {
        BlockProject();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).RestoreLostGroup(LostGroupId, "Потерянная"));

        FindGroup(LostId).ShouldBeNull();
    }

    /// <summary>Мастер проекта, которому не дали права редактировать персонажей и группы.</summary>
    private User CreateMasterWithoutEditRoles()
    {
        var master = mock.CreateMaster();
        mock.Project.ProjectAcls.Single(a => a.UserId == master.UserId).CanEditRoles = false;
        return master;
    }

    [Fact]
    public async Task LostGroup_MasterWithoutCanEditRoles_Throws_AndNothingIsInserted()
    {
        var master = CreateMasterWithoutEditRoles();
        BlockProject();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(master.UserId).RestoreLostGroup(LostGroupId, "Потерянная"));

        FindGroup(LostId).ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LostGroup_EmptyName_Throws_AndNothingIsInserted(string name)
    {
        BlockProject();

        _ = await Should.ThrowAsync<JoinValidationException>(() => CreateService().RestoreLostGroup(LostGroupId, name));

        FindGroup(LostId).ShouldBeNull();
    }

    [Fact]
    public async Task LostGroup_IsInsertedPrivate_UnderRoot_WithGivenId()
    {
        BlockProject();
        var before = DateTime.UtcNow;

        await CreateService().RestoreLostGroup(LostGroupId, "Потерянная");

        var group = FindGroup(LostId).ShouldNotBeNull();
        group.CharacterGroupName.ShouldBe("Потерянная");
        group.IsPublic.ShouldBeFalse();
        group.IsActive.ShouldBeTrue();
        group.IsSpecial.ShouldBeFalse();
        group.IsRoot.ShouldBeFalse();
        group.ParentCharacterGroupIds.ShouldBe([RootGroupId]);
        group.CreatedById.ShouldBe(mock.Master.UserId);
        group.UpdatedAt.ShouldBeGreaterThanOrEqualTo(before);
        SentInOrder.ShouldBeEmpty();
    }

    [Fact]
    public async Task LostGroup_ExistingInProject_DoesNothing()
    {
        BlockProject();
        var existing = mock.Group;
        var nameBefore = existing.CharacterGroupName;
        var countBefore = mock.Project.CharacterGroups.Count;

        await CreateService().RestoreLostGroup(new CharacterGroupIdentification(ProjectId, existing.CharacterGroupId), "Другое имя");

        existing.CharacterGroupName.ShouldBe(nameBefore);
        mock.Project.CharacterGroups.Count.ShouldBe(countBefore);
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task LostGroup_RepeatedCall_DoesNothing()
    {
        BlockProject();
        await CreateService().RestoreLostGroup(LostGroupId, "Потерянная");
        var countAfterFirst = mock.Project.CharacterGroups.Count;

        await CreateService().RestoreLostGroup(LostGroupId, "Потерянная");

        mock.Project.CharacterGroups.Count.ShouldBe(countAfterFirst);
    }

    [Fact]
    public async Task LostGroup_IdNotBelowIdentity_Throws()
    {
        BlockProject();
        var id = Projects.FakeProjectMetadataWriteRepository.GroupIdentityCurrent;

        _ = await Should.ThrowAsync<JoinValidationException>(
            () => CreateService().RestoreLostGroup(new CharacterGroupIdentification(ProjectId, id), "Будущая"));

        FindGroup(id).ShouldBeNull();
    }

    #endregion

    #region RestoreLostCharacter

    private CharacterIdentification LostCharacterId => new(ProjectId, LostId);

    private Character? FindCharacter(int id) => mock.Project.Characters.SingleOrDefault(c => c.CharacterId == id);

    private static RestoredCharacterTarget EmptyTarget => new(null, null);

    [Fact]
    public async Task LostCharacter_ActiveProject_Throws_AndNothingIsInserted()
    {
        _ = await Should.ThrowAsync<JoinValidationException>(
            () => CreateService().RestoreLostCharacter(LostCharacterId, "Потерянный", EmptyTarget));

        FindCharacter(LostId).ShouldBeNull();
    }

    [Fact]
    public async Task LostCharacter_ByPlayer_Throws_AndNothingIsInserted()
    {
        BlockProject();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).RestoreLostCharacter(LostCharacterId, "Потерянный", EmptyTarget));

        FindCharacter(LostId).ShouldBeNull();
    }

    [Fact]
    public async Task LostCharacter_MasterWithoutCanEditRoles_Throws_AndNothingIsInserted()
    {
        var master = CreateMasterWithoutEditRoles();
        BlockProject();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(master.UserId).RestoreLostCharacter(LostCharacterId, "Потерянный", EmptyTarget));

        FindCharacter(LostId).ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LostCharacter_EmptyName_Throws_AndNothingIsInserted(string name)
    {
        BlockProject();

        _ = await Should.ThrowAsync<JoinValidationException>(
            () => CreateService().RestoreLostCharacter(LostCharacterId, name, EmptyTarget));

        FindCharacter(LostId).ShouldBeNull();
    }

    [Fact]
    public async Task LostCharacter_ExistingWithoutNameField_KeepsName()
    {
        var field = AddCharacterField("Поле");
        BlockProject();
        mock.ProjectInfo.CharacterNameField.ShouldBeNull();
        var nameBefore = mock.Character.CharacterName;

        await CreateService().RestoreLostCharacter(
            mock.Character.GetId(), "Другое имя", new RestoredCharacterTarget(null, Fields((field, "из выгрузки"))));

        mock.Character.CharacterName.ShouldBe(nameBefore);
        GetCharacterValue(mock.Character, field).ShouldBe("из выгрузки");
    }

    [Fact]
    public async Task LostCharacter_ClaimField_ThrowsBeforeInserting()
    {
        var claimField = AddCharacterField("Поле заявки", boundTo: FieldBoundTo.Claim);
        BlockProject();

        _ = await Should.ThrowAsync<ArgumentException>(
            () => CreateService().RestoreLostCharacter(
                LostCharacterId, "Потерянный", new RestoredCharacterTarget(null, Fields((claimField, "значение")))));

        FindCharacter(LostId).ShouldBeNull();
    }

    [Fact]
    public async Task LostCharacter_IsInsertedHiddenPlayer_WithFieldsAndGroups()
    {
        var field = AddCharacterField("Поле");
        BlockProject();
        var before = DateTime.UtcNow;

        await CreateService().RestoreLostCharacter(
            LostCharacterId,
            "Потерянный",
            new RestoredCharacterTarget(
                [new CharacterGroupIdentification(ProjectId, mock.Group.CharacterGroupId)],
                Fields((field, "из выгрузки"))));

        var character = FindCharacter(LostId).ShouldNotBeNull();
        character.CharacterName.ShouldBe("Потерянный");
        character.CharacterType.ShouldBe(CharacterType.Player);
        character.IsPublic.ShouldBeFalse();
        character.IsActive.ShouldBeTrue();
        character.ParentCharacterGroupIds.ShouldBe([mock.Group.CharacterGroupId]);
        GetCharacterValue(character, field).ShouldBe("из выгрузки");
        character.UpdatedAt.ShouldBeGreaterThanOrEqualTo(before);
        SentInOrder.ShouldBeEmpty();
    }

    [Fact]
    public async Task LostCharacter_NoFields_StaysInRootGroup()
    {
        BlockProject();

        await CreateService().RestoreLostCharacter(LostCharacterId, "Потерянный", EmptyTarget);

        var character = FindCharacter(LostId).ShouldNotBeNull();
        character.CharacterName.ShouldBe("Потерянный");
        character.ParentCharacterGroupIds.ShouldBe([RootGroupId]);
    }

    [Fact]
    public async Task LostCharacter_NameGoesToNameField_WhenProjectHasIt()
    {
        var nameField = AddCharacterField("Имя персонажа");
        mock.Project.Details.CharacterNameField = mock.Project.ProjectFields.Single(f => f.ProjectFieldId == nameField.Id.ProjectFieldId);
        BlockProject();

        await CreateService().RestoreLostCharacter(LostCharacterId, "Потерянный", EmptyTarget);

        var character = FindCharacter(LostId).ShouldNotBeNull();
        character.CharacterName.ShouldBe("Потерянный");
        GetCharacterValue(character, nameField).ShouldBe("Потерянный");
    }

    [Fact]
    public async Task LostCharacter_InRestoredGroup_GroupIsVisibleInSameScope()
    {
        BlockProject();
        var service = CreateService();

        await service.RestoreLostGroup(LostGroupId, "Потерянная");
        await service.RestoreLostCharacter(LostCharacterId, "Потерянный", new RestoredCharacterTarget([LostGroupId], null));

        FindCharacter(LostId).ShouldNotBeNull().ParentCharacterGroupIds.ShouldBe([LostId]);
    }

    [Fact]
    public async Task LostCharacter_ExistingInProject_IsChanged_NotInserted()
    {
        var field = AddCharacterField("Поле");
        BlockProject();
        var countBefore = mock.Project.Characters.Count;

        await CreateService().RestoreLostCharacter(
            mock.Character.GetId(), "Другое имя", new RestoredCharacterTarget(null, Fields((field, "из выгрузки"))));

        mock.Project.Characters.Count.ShouldBe(countBefore);
        GetCharacterValue(mock.Character, field).ShouldBe("из выгрузки");
    }

    [Fact]
    public async Task LostCharacter_RepeatedCall_ChangesNothing()
    {
        var field = AddCharacterField("Поле");
        BlockProject();
        var target = new RestoredCharacterTarget(
            [new CharacterGroupIdentification(ProjectId, mock.Group.CharacterGroupId)],
            Fields((field, "из выгрузки")));
        await CreateService().RestoreLostCharacter(LostCharacterId, "Потерянный", target);
        var countAfterFirst = mock.Project.Characters.Count;
        var savesAfterFirst = SaveChangesCallCount;

        await CreateService().RestoreLostCharacter(LostCharacterId, "Потерянный", target);

        mock.Project.Characters.Count.ShouldBe(countAfterFirst);
        SaveChangesCallCount.ShouldBe(savesAfterFirst);
    }

    [Fact]
    public async Task LostCharacter_IdNotBelowIdentity_Throws()
    {
        BlockProject();
        var id = Fakes.FakeCharacterAggregateWriteRepository.CharacterIdentityCurrent;

        _ = await Should.ThrowAsync<JoinValidationException>(
            () => CreateService().RestoreLostCharacter(new CharacterIdentification(ProjectId, id), "Будущий", EmptyTarget));

        FindCharacter(id).ShouldBeNull();
    }

    #endregion
}
