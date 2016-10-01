using System.Collections.Generic;
using JoinRpg.Domain;

namespace JoinRpg.DataModel.Mocks
{
  public class MockedProject
  {
    public Project Project { get; }
    public CharacterGroup Group { get; }
    public User Player { get; } = new User() {UserId = 1};
    public User Master { get; } = new User() { UserId = 2 };
    public ProjectField MasterOnlyField { get; }
    public ProjectField CharacterField { get; }
    public Character Character { get; }

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
      };
      CharacterField = new ProjectField()
      {
        CanPlayerEdit = true,
        CanPlayerView = true,
        IsActive = true,
        FieldBoundTo = FieldBoundTo.Character,
        AvailableForCharacterGroupIds = new int[0]
      };

      var characterFieldValue = new FieldWithValue(CharacterField, "Value");
      Character = new Character
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
          ProjectAcl.CreateRootAcl(Master.UserId)
        },
        ProjectFields = new List<ProjectField>()
        {
          MasterOnlyField, CharacterField
        },
        Characters = new List<Character>() { Character },
        CharacterGroups = new List<CharacterGroup> {  Group},
        Claims = new List<Claim>()
      };

      FixProjectSubEntities(Project);
      //That needs to happen after FixProjectSubEntities(..)
      Character.JsonData = new[] { characterFieldValue }.SerializeFields();
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
  }
}
