using System;
using System.Collections.Generic;
using JoinRpg.Domain;

namespace JoinRpg.DataModel.Mocks
{
  public class MockedProject
  {
    public Project Project { get; }
    public CharacterGroup Group { get; }
    public User Player { get; } = new User() { UserId = 1, PrefferedName = "Player", Email = "player@example.com", Claims = new HashSet<Claim>()};
    public User Master { get; } = new User() { UserId = 2, PrefferedName = "Master", Email = "master@example.com", Claims = new HashSet<Claim>()};
    public ProjectField MasterOnlyField { get; }
    public ProjectField CharacterField { get; }
    public ProjectField HideForUnApprovedClaim { get; }
    public ProjectField PublicField { get; }

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
              CanPlayerEdit = true,
              CanPlayerView = true,
              IsActive = true,
              FieldBoundTo = FieldBoundTo.Character,
              ShowOnUnApprovedClaims = true,
              AvailableForCharacterGroupIds = new int[0],
          };

          HideForUnApprovedClaim = new ProjectField()
          {
              CanPlayerEdit = true,
              CanPlayerView = true,
              IsActive = true,
              FieldBoundTo = FieldBoundTo.Character,
              ShowOnUnApprovedClaims = false,
              AvailableForCharacterGroupIds = new int[0],
          };

          PublicField = new ProjectField()
          {
              CanPlayerEdit = false,
              CanPlayerView = true,
              IsPublic = true,
              IsActive = true,
              FieldBoundTo = FieldBoundTo.Character,
              AvailableForCharacterGroupIds = new int[0],
              ShowOnUnApprovedClaims = true,
          };

          Character = new Character
          {
              IsActive = true,
              IsAcceptingClaims = true,
              ParentCharacterGroupIds = new int[0],
          };

          CharacterWithoutGroup = new Character
          {
              IsActive = true,
              IsAcceptingClaims = true,
              ParentCharacterGroupIds = new int[0],
          };


          Project = new Project()
          {
              Active = true,
              IsAcceptingClaims = true,
              ProjectAcls = new List<ProjectAcl>
              {
                  ProjectAcl.CreateRootAcl(Master.UserId, isOwner: true),
              },
              ProjectFields = new List<ProjectField>()
              {
                  MasterOnlyField,
                  CharacterField,
                  HideForUnApprovedClaim,
                  PublicField,
              },
              Characters = new List<Character>() {Character, CharacterWithoutGroup},
              CharacterGroups = new List<CharacterGroup>(),
              Claims = new List<Claim>(),
              Details = new ProjectDetails(),
          };

          FixProjectSubEntities(Project);

          Group = CreateCharacterGroup(new CharacterGroup()
          {
              AvaiableDirectSlots = 1,
              HaveDirectSlots = true,
          });

          Character.ParentCharacterGroupIds = new[] {Group.CharacterGroupId};

      }

      public CharacterGroup CreateCharacterGroup(CharacterGroup characterGroup = null)
      {
          characterGroup = characterGroup ?? new CharacterGroup();

          characterGroup.Project = Project;
          characterGroup.ProjectId = Project.ProjectId;
          characterGroup.CharacterGroupId = Project.CharacterGroups.GetNextId();
          characterGroup.CharacterGroupName = characterGroup.CharacterGroupName ?? "test_" + characterGroup.CharacterGroupId;
          characterGroup.IsActive = true;
          Project.CharacterGroups.Add(characterGroup);

          return characterGroup;
      }

      public ProjectField CreateConditionalField(ProjectField field, CharacterGroup conditionGroup)
      {
          field = CreateField(field);
          field.AvailableForCharacterGroupIds = new[] {conditionGroup.CharacterGroupId};
          return field;
      }

      public ProjectField CreateField(ProjectField field = null)
      {
          field = field ?? new ProjectField();
          field.Project = Project;
          field.ProjectId = Project.ProjectId;
          field.ProjectFieldId = Project.ProjectFields.GetNextId();
          field.FieldName = field.FieldName ?? "test_" + field.ProjectFieldId;
          field.AvailableForCharacterGroupIds = Array.Empty<int>();
          field.IsActive = true;
          Project.ProjectFields.Add(field);
          return field;
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
          Project.Claims.Add(claim);
          mockUser.Claims.Add(claim);
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
        PlayerUserId = mockUser.UserId,
      };
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

      public ProjectField CreateConditionalHeader()
      {
          return CreateConditionalField(new ProjectField()
              {
                  CanPlayerEdit = true,
                  CanPlayerView = true,
                  ShowOnUnApprovedClaims = true,
                  FieldBoundTo = FieldBoundTo.Character,
                  FieldType = ProjectFieldType.Header,
                  FieldName = "Conditional",
              },
              Group);
      }

      public ProjectField CreateConditionalField()
      {
          return CreateConditionalField(new ProjectField()
              {
                  CanPlayerEdit = true,
                  CanPlayerView = true,
                  IsActive = true,
                  ShowOnUnApprovedClaims = true,
                  FieldBoundTo = FieldBoundTo.Character,
              },
              Group);
      }

      public static void AssignFieldValues(IFieldContainter mockCharacter, params FieldWithValue[] fieldWithValues)
      {
          mockCharacter.JsonData = fieldWithValues.SerializeFields();
      }
  }
}
