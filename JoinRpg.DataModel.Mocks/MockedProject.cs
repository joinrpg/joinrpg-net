using System.Collections.Generic;
using JoinRpg.Domain;

namespace JoinRpg.DataModel.Mocks
{
  public class MockedProject
  {
    public Project Project { get; }
    public CharacterGroup Group { get; }
    public User Player { get; } = new User() { UserId = 1, PrefferedName = "Player", Email = "player@example.com" };
    public User Master { get; } = new User() { UserId = 2, PrefferedName = "Master", Email = "master@example.com" };
    public ProjectField MasterOnlyField { get; }
    public ProjectField CharacterField { get; }
    public ProjectField ConditionalField { get; }
    public ProjectField HideForUnApprovedClaim { get; }
    public ProjectField PublicField { get; }
    public ProjectField ConditionalHeader { get; }

    public Character Character { get; }
    public Character CharacterWithoutGroup { get; }

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

      id = 0;
      foreach (Character field in project1.Characters)
      {
        id++;
        field.CharacterId = id;
        field.Project = project1;
        field.ProjectId = project1.ProjectId;
      }


      id = 0;
      foreach (var field in project1.CharacterGroups)
      {
        id++;
        field.CharacterGroupId = id;
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
        ShowOnUnApprovedClaims = true
      };
      CharacterField = new ProjectField()
      {
        CanPlayerEdit = true,
        CanPlayerView = true,
        IsActive = true,
        FieldBoundTo = FieldBoundTo.Character,
        ShowOnUnApprovedClaims = true,
        AvailableForCharacterGroupIds = new int[0]
      };
      ConditionalField = new ProjectField()
      {
        CanPlayerEdit = true,
        CanPlayerView = true,
        IsActive = true,
        ShowOnUnApprovedClaims = true,
        FieldBoundTo = FieldBoundTo.Character,
        AvailableForCharacterGroupIds = new int[0]
      };

      HideForUnApprovedClaim = new ProjectField()
      {
        CanPlayerEdit = true,
        CanPlayerView = true,
        IsActive = true,
        FieldBoundTo = FieldBoundTo.Character,
        ShowOnUnApprovedClaims = false,
        AvailableForCharacterGroupIds = new int[0]
      };

      PublicField = new ProjectField()
      {
        CanPlayerEdit = false,
        CanPlayerView = true,
        IsPublic = true,
        IsActive = true,
        FieldBoundTo = FieldBoundTo.Character,
        AvailableForCharacterGroupIds = new int[0],
        ShowOnUnApprovedClaims =  true,
      };

      ConditionalHeader = new ProjectField()
      {
        CanPlayerEdit = true,
        CanPlayerView = true,
        IsActive = true,
        ShowOnUnApprovedClaims = true,
        FieldBoundTo = FieldBoundTo.Character,
        AvailableForCharacterGroupIds = new int[0],
        FieldType = ProjectFieldType.Header
      };

      var characterFieldValue = new FieldWithValue(CharacterField, "Value");
      var publicFieldValue = new FieldWithValue(PublicField, "Public");
      Character = new Character
      {
        IsActive = true,
        IsAcceptingClaims = true,
        ParentCharacterGroupIds = new int[0]
      };

      CharacterWithoutGroup = new Character
      {
        IsActive = true,
        IsAcceptingClaims = true,
        ParentCharacterGroupIds = new int[0]
      };


      Group = new CharacterGroup()
      {
        AvaiableDirectSlots = 0,
        HaveDirectSlots = true
      };

      Project = new Project()
      {
        Active = true,
        IsAcceptingClaims = true,
        ProjectAcls = new List<ProjectAcl>
        {
          ProjectAcl.CreateRootAcl(Master.UserId, isOwner: true)
        },
        ProjectFields = new List<ProjectField>()
        {
          MasterOnlyField, CharacterField, ConditionalField, HideForUnApprovedClaim, PublicField, ConditionalHeader
        },
        Characters = new List<Character>() { Character, CharacterWithoutGroup },
        CharacterGroups = new List<CharacterGroup> {  Group},
        Claims = new List<Claim>()
      };

      FixProjectSubEntities(Project);
      //That needs to happen after FixProjectSubEntities(..)
      Character.JsonData = new[] { characterFieldValue, publicFieldValue }.SerializeFields();

      Character.ParentCharacterGroupIds = new[] {Group.CharacterGroupId};

      ConditionalField.AvailableForCharacterGroupIds = new[] {Group.CharacterGroupId};
      ConditionalHeader.AvailableForCharacterGroupIds = new[] { Group.CharacterGroupId };
    }

    public Claim CreateClaim(Character mockCharacter, User mockUser)
    {
      var claim = new Claim
      {
        Project = Project,
        Character = mockCharacter,
        CharacterId = mockCharacter.CharacterId,
        Player = mockUser,
        PlayerUserId = mockUser.UserId
      };
      Project.Claims.Add(claim);
      return claim;
    }

    public Claim CreateClaim(CharacterGroup mockGroup, User mockUser)
    {
      var claim = new Claim
      {
        Project = Project,
        CharacterGroupId = mockGroup.CharacterGroupId,
        Group =  mockGroup,
        Player = mockUser,
        PlayerUserId = mockUser.UserId
      };
      Project.Claims.Add(claim);
      return claim;
    }

    public Claim CreateApprovedClaim(Character character, User player)
    {
      var claim = CreateClaim(character, player);
      claim.ClaimStatus = Claim.Status.Approved;
      character.ApprovedClaim = claim;
      return claim;
    }
  }
}
