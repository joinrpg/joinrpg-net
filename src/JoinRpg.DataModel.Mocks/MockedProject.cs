using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.DataModel.Mocks;

public class MockedProject
{
    public Project Project { get; }
    public CharacterGroup Group { get; }
    public User Player { get; } = new User() { UserId = 1, PrefferedName = "Player", Email = "player@example.com", Claims = new HashSet<Claim>() };
    public User Master { get; } = new User() { UserId = 2, PrefferedName = "Master", Email = "master@example.com", Claims = new HashSet<Claim>() };

    public ProjectFieldInfo MasterOnlyFieldInfo { get; set; }
    public ProjectFieldInfo HideForUnApprovedClaimInfo { get; set; }

    public ProjectFieldInfo CharacterFieldInfo { get; set; }
    public ProjectFieldInfo PublicFieldInfo { get; set; }

    public Character Character { get; }

    public ProjectInfo ProjectInfo { get; private set; }

    public MockedProject()
    {
        Project = new Project()
        {
            Active = true,
            IsAcceptingClaims = true,
            ProjectAcls = [ProjectAcl.CreateRootAcl(Master.UserId, isOwner: true),],
            ProjectFields = [],
            Characters = [],
            CharacterGroups = [],
            Claims = [],
            Details = new ProjectDetails(),
            PaymentTypes = [],
        };

        var rootGroup = CreateCharacterGroup();
        rootGroup.IsRoot = true;

        ProjectInfo = ProjectRepository.CreateInfoFromProject(Project, new(Project.ProjectId));

        MasterOnlyFieldInfo = CreateField("Master only", canPlayerEdit: false, canPlayerView: false, showOnUnApprovedClaims: true);
        CharacterFieldInfo = CreateField("Visible & Editorable field", canPlayerEdit: true, canPlayerView: true, showOnUnApprovedClaims: true);
        HideForUnApprovedClaimInfo = CreateField("Hide on unapproved", canPlayerView: true, canPlayerEdit: true, showOnUnApprovedClaims: false);
        PublicFieldInfo = CreateField("Public", canPlayerView: true, canPlayerEdit: false, showOnUnApprovedClaims: true, isPublic: true);

        Group = CreateCharacterGroup();

        Character = CreateCharacter("Some Character");
    }

    public ProjectFieldInfo CreateField(string name, bool canPlayerEdit = false,
        bool canPlayerView = false, bool showOnUnApprovedClaims = false, bool isPublic = false,
        int[]? availForIds = null, ProjectFieldType fieldType = ProjectFieldType.String,
        MandatoryStatus mandatoryStatus = MandatoryStatus.Optional)
    {
        availForIds ??= [];
        if (isPublic && !canPlayerView)
        {
            throw new InvalidOperationException();
        }
        if (canPlayerEdit && !canPlayerView)
        {
            throw new InvalidOperationException();
        }
        var id = new ProjectFieldIdentification(ProjectInfo.ProjectId, ProjectInfo.UnsortedFields.GetNextId());
        var field = new ProjectFieldInfo(id, name, fieldType, FieldBoundTo.Character, [], "", 0, CanPlayerEdit: canPlayerEdit, CanPlayerView: canPlayerView,
            ShowOnUnApprovedClaims: showOnUnApprovedClaims, IsPublic: isPublic, MandatoryStatus: mandatoryStatus, ValidForNpc: true, IsActive: true,
            GroupsAvailableForIds: availForIds,
            Description: new MarkdownString(), MasterDescription: new MarkdownString(),
            IncludeInPrint: true,
            FieldSettings: ProjectInfo.ProjectFieldSettings, ProgrammaticValue: null);

        ProjectFieldInfo[] fields = [field, .. ProjectInfo.UnsortedFields];

        ProjectInfo = new ProjectInfo(ProjectInfo.ProjectId, ProjectInfo.ProjectName, ProjectInfo.FieldsOrdering, fields, ProjectInfo.ProjectFieldSettings, ProjectInfo.ProjectFinanceSettings, ProjectInfo.AccomodationEnabled, ProjectInfo.DefaultTemplateCharacter, ProjectInfo.AllowToSetGroups, ProjectInfo.RootCharacterGroupId);
        return field;
    }

    public void ReInitProjectInfo()
    {
        ProjectInfo = ProjectRepository.CreateInfoFromProject(Project, new(Project.ProjectId));
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
        return CreateField("CondField", availForIds: [conditionGroup.CharacterGroupId]);
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
        return CreateField("", canPlayerEdit: true, canPlayerView: true, showOnUnApprovedClaims: true, availForIds: [characterGroup.CharacterGroupId], fieldType: ProjectFieldType.Header);
    }

    public ProjectFieldInfo CreateConditionalField()
    {
        return CreateField("", canPlayerEdit: true, canPlayerView: true, showOnUnApprovedClaims: true, availForIds: [Group.CharacterGroupId]);
    }

    public static void AssignFieldValues(IFieldContainter mockCharacter, params FieldWithValue[] fieldWithValues) => mockCharacter.JsonData = fieldWithValues.SerializeFields();

    public static void AddCharToGroup(DataModel.Character character, DataModel.CharacterGroup group) => character.ParentCharacterGroupIds = [.. character.ParentCharacterGroupIds.Union([group.CharacterGroupId])];
}
