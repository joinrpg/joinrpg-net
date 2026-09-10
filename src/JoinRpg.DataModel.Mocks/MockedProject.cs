using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.DataModel.Extensions;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.DataModel.Mocks;

public class MockedProject
{
    public Project Project { get; }
    public CharacterGroup Group { get; }
    public User Player { get; } = new User() { UserId = 1, PrefferedName = "Player", Email = "player@example.com", Claims = new HashSet<Claim>() };
    private UserInfo PlayerInfoTemplate { get; } = new UserInfo(new UserIdentification(1), Social: new UserSocialNetworks(null, null, null, null, ContactsAccessType.Public), [], [], [], IsAdmin: false, SelectedAvatarId: null, new Email("player@example.com"), EmailConfirmed: true, new UserFullName(new PrefferedName("Player"), null, null, null), false, null, HasPassword: false);

    /// <summary>
    /// <see cref="UserInfo"/> игрока, согласованный с заявками мока.
    /// </summary>
    /// <remarks>
    /// Именно свойство, а не поле: правила заявки читают <see cref="UserInfo.ActiveClaims"/>, и
    /// если зафиксировать его на момент создания мока, заявки, созданные тестом позже, туда не
    /// попадут — а правила молча решат, что заявок у игрока нет.
    /// </remarks>
    public UserInfo PlayerInfo => PlayerInfoTemplate with
    {
        ActiveClaims = [.. Player.Claims
            .Where(claim => claim.ClaimStatus.IsActive())
            .Select(claim => new UserClaimInfo(claim.GetId(), claim.ClaimStatus))],
    };
    public User Master { get; } = new User() { UserId = 2, PrefferedName = "Master", Email = "master@example.com", Claims = new HashSet<Claim>() };

    public ProjectFieldInfo MasterOnlyFieldInfo { get; set; }
    public ProjectFieldInfo HideForUnApprovedClaimInfo { get; set; }

    public ProjectFieldInfo CharacterFieldInfo { get; set; }
    public ProjectFieldInfo PublicFieldInfo { get; set; }

    public Character Character { get; }

    public ProjectInfo ProjectInfo { get; private set; }

    public MockedProject()
    {
        var acl = ProjectAcl.CreateRootAcl(Master.UserId, isOwner: true);
        acl.User = Master;
        Project = new Project()
        {
            Active = true,
            IsAcceptingClaims = true,
            ProjectAcls = [acl,],
            ProjectFields = [],
            Characters = [],
            CharacterGroups = [],
            Claims = [],
            Details = new ProjectDetails(),
            PaymentTypes = [],
            ProjectFeeSettings = [],
            ProjectName = "Mocked project",
            KogdaIgraGames = [],
            ProjectRolesLists = [],
        };

        var rootGroup = CreateCharacterGroup();
        rootGroup.IsRoot = true;
        Group = CreateCharacterGroup();

        ProjectInfo = ProjectMetadataRepository.CreateInfoFromProject(Project, new(Project.ProjectId));

        MasterOnlyFieldInfo = CreateField("Master only", canPlayerEdit: false, showOnUnApprovedClaims: true, projectFieldVisibility: ProjectFieldVisibility.MasterOnly);
        CharacterFieldInfo = CreateField("Visible & Editorable field", canPlayerEdit: true, showOnUnApprovedClaims: true);
        HideForUnApprovedClaimInfo = CreateField("Hide on unapproved", canPlayerEdit: true, showOnUnApprovedClaims: false);
        PublicFieldInfo = CreateField("Public", projectFieldVisibility: ProjectFieldVisibility.Public, canPlayerEdit: false, showOnUnApprovedClaims: true);



        Character = CreateCharacter("Some Character");
    }

    public ProjectFieldInfo CreateField(string name, ProjectFieldVisibility projectFieldVisibility = ProjectFieldVisibility.PlayerAndMaster, bool canPlayerEdit = false,
        bool showOnUnApprovedClaims = false, bool isPublic = false,
        CharacterGroupIdentification[]? availForIds = null, ProjectFieldType fieldType = ProjectFieldType.String,
        MandatoryStatus mandatoryStatus = MandatoryStatus.Optional)
    {
        availForIds ??= [];
        if (canPlayerEdit && projectFieldVisibility != ProjectFieldVisibility.PlayerAndMaster)
        {
            throw new InvalidOperationException();
        }
        var id = new ProjectFieldIdentification(ProjectInfo.ProjectId, ProjectInfo.UnsortedFields.GetNextId());
        var field = new ProjectFieldInfo(id, name, fieldType, FieldBoundTo.Character, [], "", 0, CanPlayerEdit: canPlayerEdit, ShowOnUnApprovedClaims: showOnUnApprovedClaims,
            MandatoryStatus: mandatoryStatus, ValidForNpc: true, IsActive: true, GroupsAvailableForIds: availForIds, Description: new MarkdownDbValue(),
            MasterDescription: new MarkdownDbValue(),
            IncludeInPrint: true, FieldSettings: ProjectInfo.ProjectFieldSettings,
            ProgrammaticValue: null,
            ProjectFieldVisibility: projectFieldVisibility,
            SpecialGroupId: null,
            WasEverUsed: false
            );

        ProjectInfo = ProjectInfo.WithAddedField(field);

        return field;
    }

    public void ReInitProjectInfo()
    {
        // Имитация перечитывания Project из БД (см. ProjectMetadataWriteRepository.Refresh): в бою
        // свежий запрос с Include подтягивает navigation-свойства даже тем сущностям, которые были
        // добавлены в рамках мутации "на лету" (например новый ProjectAcl без явно проставленного
        // User) — lazy loading для них не сработал бы, а реальный реload сработает.
        foreach (var acl in Project.ProjectAcls)
        {
            acl.User ??= new User { UserId = acl.UserId, PrefferedName = $"User{acl.UserId}", Email = $"user{acl.UserId}@example.com", Claims = [] };
        }

        foreach (var paymentType in Project.PaymentTypes)
        {
            paymentType.User ??= Project.ProjectAcls.FirstOrDefault(a => a.UserId == paymentType.UserId)?.User
                ?? new User { UserId = paymentType.UserId, PrefferedName = $"User{paymentType.UserId}", Email = $"user{paymentType.UserId}@example.com", Claims = [] };
        }

        // Имитация relationship fixup EF6: если дефолтная сетка ролей проставлена только
        // навигационным свойством (сущность добавлена в рамках текущей мутации и ещё не имела Id),
        // подтягиваем сгенерированный Id в FK-свойство — как это делает реальный DbContext при SaveChanges.
        if (Project.Details.DefaultProjectRolesList is { } defaultRolesList)
        {
            Project.Details.DefaultProjectRolesListId = defaultRolesList.ProjectRolesListId;
        }

        ProjectInfo = ProjectMetadataRepository.CreateInfoFromProject(Project, new(Project.ProjectId));
    }

    public CharacterGroup CreateCharacterGroup(bool skipReinit = false)
    {
        var id = Project.CharacterGroups.GetNextId();
        var characterGroup = new CharacterGroup
        {
            Project = Project,
            ProjectId = Project.ProjectId,

            CharacterGroupId = id,
            CharacterGroupName = "test_" + id,
            IsActive = true,
        };
        Project.CharacterGroups.Add(characterGroup);

        return characterGroup;
    }

    public Character CreateCharacter(string name)
    {
        var character = new Character
        {
            IsActive = true,
            IsAcceptingClaims = true,
            ParentCharacterGroupIds = [Project.CharacterGroups.Single(x => x.IsRoot).CharacterGroupId],
            CharacterId = Project.Characters.GetNextId(),
            Claims = [],
            Project = Project,
            CharacterName = name,
        };

        Project.Characters.Add(character);

        return character;
    }

    public ProjectFieldInfo CreateConditionalField(CharacterGroup conditionGroup)
    {
        return CreateField("CondField", availForIds: [new(ProjectInfo.ProjectId, conditionGroup.CharacterGroupId)]);
    }

    public ProjectFieldInfo AddField(Action<ProjectField> setup)
    {
        var field = new ProjectField();
        field.Project = Project;
        field.ProjectId = Project.ProjectId;
        field.ProjectFieldId = Project.ProjectFields.GetNextId();
        field.FieldName ??= "test_" + field.ProjectFieldId;
        field.AvailableForCharacterGroupIds = [];
        field.IsActive = true;
        setup(field);
        Project.ProjectFields.Add(field);
        ReInitProjectInfo();
        return ProjectInfo.GetFieldById(new ProjectFieldIdentification(new ProjectIdentification(Project.ProjectId), field.ProjectFieldId));
    }

    /// <summary>
    /// Доменный агрегат (ADR013) для персонажа мока — зеркало <c>CharacterInfoMapper</c>.
    /// </summary>
    /// <remarks>
    /// Именно фабрика, кешировать нельзя: <see cref="ReInitProjectInfo"/> и
    /// <see cref="CreateField"/> подменяют экземпляр <see cref="ProjectInfo"/>, а конструктор
    /// <see cref="CharacterInfo"/> требует, чтобы слои полей были привязаны ровно к тому же
    /// экземпляру. По той же причине вызывать надо после того, как все поля проекта заведены.
    /// </remarks>
    public CharacterInfo GetCharacterInfo(Character character)
    {
        var projectId = ProjectInfo.ProjectId;

        return new CharacterInfo(
            new CharacterIdentification(projectId, character.CharacterId),
            ProjectInfo,
            character.CharacterName,
            character.ToCharacterTypeInfo(),
            character.HidePlayerForCharacter,
            character.IsActive,
            character.InGame,
            character.AutoCreated,
            new MarkdownString(character.Description?.Contents ?? ""),
            originalCharacterSlotId: null,
            [.. character.ParentCharacterGroupIds.Select(id => new CharacterGroupIdentification(projectId, id))],
            FieldLayerContainer.DeserializeFieldLayer(ProjectInfo, character.JsonData),
            [.. character.Claims.Select(GetClaimInfo)],
            ClaimIdentification.FromOptional(projectId, character.ApprovedClaimId),
            character.CreatedAt,
            new UserIdentification(Master.UserId),
            character.UpdatedAt,
            new UserIdentification(Master.UserId));
    }

    private CharacterClaimInfo GetClaimInfo(Claim claim)
        => new(
            claim.GetId(),
            new UserInfoHeader(
                new UserIdentification(claim.PlayerUserId),
                new UserDisplayName(new PrefferedName(claim.Player.PrefferedName), new Email(claim.Player.Email))),
            claim.ClaimStatus,
            claim.ClaimDenialStatus,
            // В моке ответственный мастер обычно не проставлен, а UserIdentification нулю не рад.
            new UserIdentification(claim.ResponsibleMasterUserId == 0 ? Master.UserId : claim.ResponsibleMasterUserId),
            claim.CreateDate,
            claim.LastUpdateDateTime,
            claim.CheckInDate,
            claim.LastPlayerCommentAt,
            claim.LastMasterCommentAt,
            claim.LastVisibleMasterCommentAt,
            claim.CurrentFee,
            claim.PreferentialFeeUser,
            FeePaid: 0,
            AccommodationFee: 0,
            FieldLayerContainer.DeserializeFieldLayer(ProjectInfo, claim.JsonData));

    public Claim CreateClaim(Character mockCharacter, User mockUser)
    {
        var claim = new Claim
        {
            ClaimId = Project.Claims.GetNextId(),
            Project = Project,
            Character = mockCharacter,
            CharacterId = mockCharacter.CharacterId,
            Player = mockUser,
            PlayerUserId = mockUser.UserId,
        };
        mockCharacter.Claims.Add(claim);
        Project.Claims.Add(claim);
        mockUser.Claims.Add(claim);
        return claim;
    }

    public Claim CreateApprovedClaim(Character character, User player)
    {
        var claim = CreateClaim(character, player);
        claim.ClaimStatus = ClaimStatus.Approved;
        character.ApprovedClaim = claim;
        character.ApprovedClaimId = claim.ClaimId;
        return claim;
    }

    public Claim CreateCheckedInClaim(Character character, User player)
    {
        var claim = CreateClaim(character, player);
        claim.ClaimStatus = ClaimStatus.CheckedIn;
        character.ApprovedClaim = claim;
        character.ApprovedClaimId = claim.ClaimId;
        return claim;
    }

    public ProjectFieldInfo CreateConditionalHeader(CharacterGroup characterGroup)
    {
        return CreateField("", canPlayerEdit: true, showOnUnApprovedClaims: true, availForIds: [new(ProjectInfo.ProjectId, characterGroup.CharacterGroupId)], fieldType: ProjectFieldType.Header);
    }

    public ProjectFieldInfo CreateConditionalField()
    {
        return CreateField("", canPlayerEdit: true, showOnUnApprovedClaims: true, availForIds: [new(ProjectInfo.ProjectId, Group.CharacterGroupId)]);
    }

    public static void AssignFieldValues(IFieldContainter mockCharacter, params FieldWithValue[] fieldWithValues) => mockCharacter.JsonData = fieldWithValues.SerializeFields();

    public static void AddCharToGroup(DataModel.Character character, DataModel.CharacterGroup group) => character.ParentCharacterGroupIds = [.. character.ParentCharacterGroupIds.Union([group.CharacterGroupId])];
}
