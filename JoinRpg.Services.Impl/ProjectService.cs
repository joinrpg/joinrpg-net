using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Dal.Impl;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
  [UsedImplicitly]
  public class ProjectService : DbServiceImplBase, IProjectService
  {
    public ProjectService(IUnitOfWork unitOfWork) : base(unitOfWork)
    {

    }

    public Project AddProject(string projectName, User creator)
    {
      var project = new Project()
      {
        Active = true,
        CreatedDate = DateTime.Now,
        ProjectName = projectName,
        CharacterGroups = new List<CharacterGroup>()
        {
          new CharacterGroup()
          {
            IsPublic = true,
            IsRoot = true,
            CharacterGroupName = "Все персонажи на игре",
            IsActive = true
          }
        },
        ProjectAcls = new List<ProjectAcl>()
        {
          ProjectAcl.CreateRootAcl(creator.UserId)
        }
      };
      UnitOfWork.GetDbSet<Project>().Add(project);
      UnitOfWork.SaveChanges();
      return project;
    }

    public void AddCharacterField(ProjectCharacterField field)
    {
      field.IsActive = true;
      CheckField(field);
      UnitOfWork.GetDbSet<ProjectCharacterField>().Add(field);
      UnitOfWork.SaveChanges();
    }

    private static void CheckField(ProjectCharacterField field)
    {
      if (field.IsPublic && !field.CanPlayerView)
      {
        throw new DbEntityValidationException();
      }

      field.FieldName = Required(field.FieldName);
    }

    public void UpdateCharacterField(int projectId, int fieldId, string name, string fieldHint,
      bool canPlayerEdit, bool canPlayerView, bool isPublic)
    {
      var field = LoadProjectSubEntity<ProjectCharacterField>(projectId, fieldId);
      field.FieldName = name;
      field.FieldHint = fieldHint;
      field.CanPlayerEdit = canPlayerEdit;
      field.CanPlayerView = canPlayerView;
      field.IsPublic = isPublic;

      CheckField(field);

      UnitOfWork.SaveChanges();
    }

    //TODO: pass projectId and use LoadProjectSubEntity here
    public void DeleteField(int projectCharacterFieldId)
    {
      var field = UnitOfWork.GetDbSet<ProjectCharacterField>().Find(projectCharacterFieldId);
      SmartDelete(field);
      UnitOfWork.SaveChanges();
    }

    public void AddCharacterGroup(int projectId, string name, bool isPublic, List<int> parentCharacterGroupIds,
      string description)
    {
      var characterGroups = ValidateCharacterGroupList(projectId, parentCharacterGroupIds);

      if (string.IsNullOrWhiteSpace(name))
      {
        throw new DbEntityValidationException();
      }

      UnitOfWork.GetDbSet<CharacterGroup>().Add(new CharacterGroup()
      {
        AvaiableDirectSlots = 0,
        HaveDirectSlots = false,
        CharacterGroupName = name,
        ParentGroups = characterGroups,
        ProjectId = projectId,
        IsRoot = false,
        IsPublic = isPublic,
        IsActive = true,
        Description = new MarkdownString(description)
      });
      UnitOfWork.SaveChanges();
    }

    private List<CharacterGroup> ValidateCharacterGroupList(int projectId, List<int> parentCharacterGroupIds)
    {
      if (parentCharacterGroupIds.Count == 0)
      {
        throw new DbEntityValidationException();
      }

      var characterGroups =
        UnitOfWork.GetDbSet<CharacterGroup>().Where(cg => cg.ProjectId == projectId)
          .Where(cg => parentCharacterGroupIds.Contains(cg.CharacterGroupId))
          .ToList();

      if (characterGroups.Count != parentCharacterGroupIds.Distinct().Count())
      {
        throw new DbEntityValidationException();
      }
      return characterGroups;
    }

    public void AddCharacter(int projectId, string name, bool isPublic, List<int> parentCharacterGroupIds,
      bool isAcceptingClaims, string description)
    {
      var characterGroups = ValidateCharacterGroupList(projectId, parentCharacterGroupIds);

      if (string.IsNullOrWhiteSpace(name))
      {
        throw new DbEntityValidationException();
      }

      UnitOfWork.GetDbSet<Character>().Add(
        new Character
        {
          CharacterName = name,
          Groups = characterGroups,
          ProjectId = projectId,
          IsPublic = isPublic,
          IsActive = true,
          IsAcceptingClaims = isAcceptingClaims,
          Description = new MarkdownString(description)
        });
      UnitOfWork.SaveChanges();
    }

    public void EditCharacterGroup(int projectId, int characterGroupId, string name, bool isPublic,
      List<int> parentCharacterGroupIds,
      string description, bool haveDirectSlots, int directSlots)
    {
      var characterGroup = LoadProjectSubEntity<CharacterGroup>(projectId, characterGroupId);
      var parentGroups = ValidateCharacterGroupList(projectId, parentCharacterGroupIds);
      characterGroup.CharacterGroupName = Required(name);
      characterGroup.IsPublic = isPublic;
      characterGroup.ParentGroups.AssignLinksList(parentGroups);
      characterGroup.Description = new MarkdownString(description);
      characterGroup.AvaiableDirectSlots = directSlots;
      characterGroup.HaveDirectSlots = haveDirectSlots;
      UnitOfWork.SaveChanges();
    }

    public void DeleteCharacterGroup(int projectId, int characterGroupId)
    {
      var characterGroup = LoadProjectSubEntity<CharacterGroup>(projectId, characterGroupId);
      ReparentChilds(characterGroup, characterGroup.ChildGroups);
      ReparentChilds(characterGroup, characterGroup.Characters);
      if (characterGroup.CanBePermanentlyDeleted)
      {
        characterGroup.DirectlyRelatedPlotFolders.CleanLinksList();
        characterGroup.DirectlyRelatedPlotElements.CleanLinksList();
        characterGroup.ParentGroups.CleanLinksList();
      }
      SmartDelete(characterGroup);
      UnitOfWork.SaveChanges();
    }

    public void EditProject(int projectId, string projectName, string claimApplyRules)
    {
      var project = UnitOfWork.GetDbSet<Project>().Find(projectId);
      if (project.Details == null)
      {
        project.Details = new ProjectDetails {ProjectId = projectId};
      }
      project.Details.ClaimApplyRules = new MarkdownString(claimApplyRules);
      project.ProjectName = Required(projectName);
      UnitOfWork.SaveChanges();
    }

    public void GrantAccess(int projectId, int userId, bool canGrantRights, bool canChangeFields, bool canChangeProjectProperties)
    {
      var project = UnitOfWork.GetDbSet<Project>().Find(projectId);
      var acl = project.ProjectAcls.SingleOrDefault(a => a.UserId == userId);
      if (acl == null)
      {
        acl = new ProjectAcl() {ProjectId = project.ProjectId, UserId = userId};
        project.ProjectAcls.Add(acl);
      }
      acl.CanGrantRights = canGrantRights;
      acl.CanChangeFields = canChangeFields;
      acl.CanChangeProjectProperties = canChangeProjectProperties;
      UnitOfWork.SaveChanges();
    }

    public void SaveCharacterFields(int projectId, int characterId, int currentUserId, string characterName, IDictionary<int, string> newFieldValue)
    {
      var character = LoadProjectSubEntity<Character>(projectId, characterId);
      var fields = character.Fields();

      var hasMasterAccess = character.Project.HasAccess(currentUserId);
      var hasPlayerAccess = character.ApprovedClaim?.PlayerUserId == currentUserId;

      if (!hasMasterAccess && !hasPlayerAccess)
      {
        throw new DbEntityValidationException();
      }

      if (hasMasterAccess)
      {
        character.CharacterName = Required(characterName);
      }
      

      foreach (var keyValuePair in newFieldValue)
      {
        CharacterFieldValue field;
        if (!fields.TryGetValue(keyValuePair.Key, out field))
        {
          throw new DbEntityValidationException();
        }
        var newValue = keyValuePair.Value;

        if (!field.Field.CanPlayerEdit && !hasMasterAccess)
        {
          throw new DbEntityValidationException();
        }
        //TODO: typechecking here
        field.Value = newValue;
      }
      UnitOfWork.SaveChanges();
    }

    private static void ReparentChilds(CharacterGroup characterGroup, IEnumerable<IWorldObject> childs)
    {
      foreach (var child in childs)
      {
        if (characterGroup.CanBePermanentlyDeleted)
        {
          child.ParentGroups.Remove(characterGroup);
        }
        child.ParentGroups.AddLinkList(characterGroup.ParentGroups);
      }
    }
  }
}
