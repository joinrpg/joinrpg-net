using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.PrimitiveTypes.Users;

namespace JoinRpg.DataModel.Mocks;

public class MockedProject
{
    public Project Project { get; }
    public CharacterGroup Group { get; }
    public User Player { get; } = new User() { UserId = 1, PrefferedName = "Player", Email = "player@example.com", Claims = new HashSet<Claim>() };
    public UserInfo PlayerInfo = new UserInfo(new UserIdentification(1), Social: new UserSocialNetworks(null, null, null, null, null, ContactsAccessType.Public), [], [], [], IsAdmin: false, SelectedAvatarId: null, new Email("player@example.com"), new UserFullName(new PrefferedName("Player"), null, null, null), false, null);
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
            ProjectName = "Mocked project",
            KogdaIgraGames = [],
        };

        var rootGroup = CreateCharacterGroup();
        rootGroup.IsRoot = true;

        ProjectInfo = ProjectMetadataRepository.CreateInfoFromProject(Project, new(Project.ProjectId));

        MasterOnlyFieldInfo = CreateField("Master only", canPlayerEdit: false, showOnUnApprovedClaims: true, projectFieldVisibility: ProjectFieldVisibility.MasterOnly);
        CharacterFieldInfo = CreateField("Visible & Editorable field", canPlayerEdit: true, showOnUnApprovedClaims: true);
        HideForUnApprovedClaimInfo = CreateField("Hide on unapproved", canPlayerEdit: true, showOnUnApprovedClaims: false);
        PublicFieldInfo = CreateField("Public", projectFieldVisibility: ProjectFieldVisibility.Public, canPlayerEdit: false, showOnUnApprovedClaims: true);

        Group = CreateCharacterGroup();

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
            MandatoryStatus: mandatoryStatus, ValidForNpc: true, IsActive: true, GroupsAvailableForIds: availForIds, Description: new MarkdownString(),
            MasterDescription: new MarkdownString(),
            IncludeInPrint: true, FieldSettings: ProjectInfo.ProjectFieldSettings,
            ProgrammaticValue: null,
            ProjectFieldVisibility: projectFieldVisibility,
            SpecialGroupId: null
            );

        ProjectInfo = ProjectInfo.WithAddedField(field);

        return field;
    }

    public void ReInitProjectInfo()
    {
        ProjectInfo = ProjectMetadataRepository.CreateInfoFromProject(Project, new(Project.ProjectId));
    }

    public CharacterGroup CreateCharacterGroup()
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
        return ProjectInfo.GetFieldById(new PrimitiveTypes.ProjectFieldIdentification(new PrimitiveTypes.ProjectIdentification(Project.ProjectId), field.ProjectFieldId));
    }

    public Claim CreateClaim(Character mockCharacter, User mockUser)
    {
        var claim = new Claim
        {
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
        claim.ClaimStatus = Claim.Status.Approved;
        character.ApprovedClaim = claim;
        character.ApprovedClaimId = claim.ClaimId;
        return claim;
    }

    public Claim CreateCheckedInClaim(Character character, User player)
    {
        var claim = CreateClaim(character, player);
        claim.ClaimStatus = Claim.Status.CheckedIn;
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
