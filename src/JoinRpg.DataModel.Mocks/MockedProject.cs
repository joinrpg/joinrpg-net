using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.DataModel.Mocks;

public class MockedProject
{
    public Project Project { get; }
    public CharacterGroup Group { get; }
    public User Player { get; } = new User() { UserId = 1, PrefferedName = "Player", Email = "player@example.com", Claims = new HashSet<Claim>() };
    public User Master { get; } = new User() { UserId = 2, PrefferedName = "Master", Email = "master@example.com", Claims = new HashSet<Claim>() };
    public ProjectField MasterOnlyField { get; }

    public ProjectFieldInfo MasterOnlyFieldInfo { get; set; }
    public ProjectField CharacterField { get; }
    public ProjectField HideForUnApprovedClaim { get; }
    public ProjectFieldInfo HideForUnApprovedClaimInfo { get; set; }

    public ProjectFieldInfo CharacterFieldInfo { get; set; }
    public ProjectField PublicField { get; }
    public ProjectFieldInfo PublicFieldInfo { get; set; }

    public Character Character { get; }

    public ProjectInfo ProjectInfo { get; private set; }

    private static void FixProjectSubEntities(Project project1)
    {
        var id = 0;
        foreach (ProjectField field in project1.ProjectFields)
        {
            id++;
            field.ProjectFieldId = id;
            field.Project = project1;
            field.ProjectId = project1.ProjectId;
        }
    }

    public MockedProject()
    {
        MasterOnlyField = new ProjectField()
        {
            CanPlayerEdit = false,
            CanPlayerView = false,
            IsActive = true,
            ShowOnUnApprovedClaims = true,
        };
        CharacterField = new ProjectField()
        {
            FieldName = "Visible & Editorable field",
            CanPlayerEdit = true,
            CanPlayerView = true,
            IsActive = true,
            FieldBoundTo = FieldBoundTo.Character,
            ShowOnUnApprovedClaims = true,
            AvailableForCharacterGroupIds = [],
        };

        HideForUnApprovedClaim = new ProjectField()
        {
            CanPlayerEdit = true,
            CanPlayerView = true,
            IsActive = true,
            FieldBoundTo = FieldBoundTo.Character,
            ShowOnUnApprovedClaims = false,
            AvailableForCharacterGroupIds = [],
        };

        PublicField = new ProjectField()
        {
            CanPlayerEdit = false,
            CanPlayerView = true,
            IsPublic = true,
            IsActive = true,
            FieldBoundTo = FieldBoundTo.Character,
            AvailableForCharacterGroupIds = [],
            ShowOnUnApprovedClaims = true,
        };

        Project = new Project()
        {
            Active = true,
            IsAcceptingClaims = true,
            ProjectAcls = [ProjectAcl.CreateRootAcl(Master.UserId, isOwner: true),],
            ProjectFields =
          [
              MasterOnlyField,
              CharacterField,
              HideForUnApprovedClaim,
              PublicField,
          ],
            Characters = [],
            CharacterGroups = [],
            Claims = [],
            Details = new ProjectDetails(),
            PaymentTypes = [],
        };

        FixProjectSubEntities(Project);

        var rootGroup = CreateCharacterGroup();
        rootGroup.IsRoot = true;

        Group = CreateCharacterGroup();

        Character = CreateCharacter("Some Character");

        ReInitProjectInfo();


    }

    public void ReInitProjectInfo()
    {
        ProjectInfo = ProjectRepository.CreateInfoFromProject(Project, new(Project.ProjectId));
        PublicFieldInfo = ProjectInfo.GetFieldById(new(ProjectInfo.ProjectId, PublicField.ProjectFieldId));
        HideForUnApprovedClaimInfo = ProjectInfo.GetFieldById(new(ProjectInfo.ProjectId, HideForUnApprovedClaim.ProjectFieldId));
        CharacterFieldInfo = ProjectInfo.GetFieldById(new PrimitiveTypes.ProjectFieldIdentification(ProjectInfo.ProjectId, CharacterField.ProjectFieldId));
        MasterOnlyFieldInfo = ProjectInfo.GetFieldById(new PrimitiveTypes.ProjectFieldIdentification(ProjectInfo.ProjectId, MasterOnlyField.ProjectFieldId));
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

    public ProjectFieldInfo CreateConditionalField(Action<ProjectField> setup, CharacterGroup conditionGroup)
    {
        return AddField(f => { f.AvailableForCharacterGroupIds = new[] { conditionGroup.CharacterGroupId }; setup(f); });
    }

    public ProjectFieldInfo CreateConditionalField(CharacterGroup conditionGroup)
    {
        return AddField(f => f.AvailableForCharacterGroupIds = new[] { conditionGroup.CharacterGroupId });
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

    public ProjectFieldInfo AddField()
    {
        return AddField(f => { });
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
        return CreateConditionalField(f =>
        {
            f.CanPlayerEdit = true;
            f.CanPlayerView = true;
            f.ShowOnUnApprovedClaims = true;
            f.FieldBoundTo = FieldBoundTo.Character;
            f.FieldType = ProjectFieldType.Header;
            f.FieldName = "Conditional";
        },
            characterGroup);
    }

    public ProjectFieldInfo CreateConditionalField()
    {
        return CreateConditionalField(f =>
        {

            f.CanPlayerEdit = true;
            f.CanPlayerView = true;
            f.IsActive = true;
            f.ShowOnUnApprovedClaims = true;
            f.FieldBoundTo = FieldBoundTo.Character;
        },
            Group);
    }

    public static void AssignFieldValues(IFieldContainter mockCharacter, params FieldWithValue[] fieldWithValues) => mockCharacter.JsonData = fieldWithValues.SerializeFields();

    public static void AddCharToGroup(DataModel.Character character, DataModel.CharacterGroup group) => character.ParentCharacterGroupIds = [.. character.ParentCharacterGroupIds.Union([group.CharacterGroupId])];
}
