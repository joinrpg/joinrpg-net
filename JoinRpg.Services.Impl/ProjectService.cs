using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
  [UsedImplicitly]
  public class ProjectService : DbServiceImplBase, IProjectService
  {
    public ProjectService(IUnitOfWork unitOfWork) : base(unitOfWork)
    {

    }

    public async Task<Project> AddProject(string projectName, User creator)
    {
      var project = new Project()
      {
        Active = true,
        CreatedDate = DateTime.UtcNow,
        ProjectName = Required(projectName),
        CharacterGroups = new List<CharacterGroup>()
        {
          new CharacterGroup()
          {
            IsPublic = true,
            IsRoot = true,
            CharacterGroupName = "Все роли",
            IsActive = true,
            ResponsibleMasterUserId = creator.UserId
          }
        },
        ProjectAcls = new List<ProjectAcl>()
        {
          ProjectAcl.CreateRootAcl(creator.UserId)
        }
      };
      UnitOfWork.GetDbSet<Project>().Add(project);
      await UnitOfWork.SaveChangesAsync();
      return project;
    }

    public async Task AddCharacterField(int projectId, int currentUserId, CharacterFieldType fieldType, string name, string fieldHint,
      bool canPlayerEdit, bool canPlayerView, bool isPublic)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);

      if (!project.HasMasterAccess(currentUserId, acl => acl.CanChangeFields))
      {
        throw new Exception();
      }
      var field = new ProjectCharacterField
      {
        FieldName = Required(name),
        FieldHint = fieldHint,
        CanPlayerEdit = canPlayerEdit,
        CanPlayerView = canPlayerView,
        IsPublic = isPublic,
        ProjectId = projectId,
        FieldType = fieldType,
        IsActive = true,
        Order = project.AllProjectFields.Count(),
      };
      CheckField(field);
      UnitOfWork.GetDbSet<ProjectCharacterField>().Add(field);
      await UnitOfWork.SaveChangesAsync();
    }

    private static void CheckField(ProjectCharacterField field)
    {
      if (!field.IsPublic || field.CanPlayerView) return;
      throw new DbEntityValidationException();
    }

    public async Task UpdateCharacterField(int projectId, int fieldId, string name, string fieldHint,
      bool canPlayerEdit, bool canPlayerView, bool isPublic)
    {
      var field = await UnitOfWork.GetDbSet<ProjectCharacterField>().FindAsync(fieldId);
      if (field == null || field.ProjectId != projectId) throw new DbEntityValidationException();
      
      field.FieldName = Required(name);
      field.FieldHint = fieldHint;
      field.CanPlayerEdit = canPlayerEdit;
      field.CanPlayerView = canPlayerView;
      field.IsPublic = isPublic;
      field.IsActive = true;

      CheckField(field);

      await UnitOfWork.SaveChangesAsync();
    }

    //TODO: pass projectId 
    public async Task DeleteField(int projectCharacterFieldId)
    {
      var field = await UnitOfWork.GetDbSet<ProjectCharacterField>().FindAsync(projectCharacterFieldId);
      SmartDelete(field);
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task AddCharacterGroup(int projectId, string name, bool isPublic, List<int> parentCharacterGroupIds, string description, bool haveDirectSlotsForSave, int directSlotsForSave, int? responsibleMasterId)
    {
      var characterGroups = await ValidateCharacterGroupList(projectId, Required(parentCharacterGroupIds));
      var project = await ProjectRepository.GetProjectAsync(projectId);

      if (responsibleMasterId != null &&
          project.ProjectAcls.All(acl => acl.UserId != responsibleMasterId))
      {
        throw new Exception("No such master");
      }

      UnitOfWork.GetDbSet<CharacterGroup>().Add(new CharacterGroup()
      {
        AvaiableDirectSlots = directSlotsForSave,
        HaveDirectSlots = haveDirectSlotsForSave,
        CharacterGroupName = Required(name),
        ParentGroups = characterGroups,
        ProjectId = projectId,
        IsRoot = false,
        IsPublic = isPublic,
        IsActive = true,
        Description = new MarkdownString(description),
        ResponsibleMasterUserId = responsibleMasterId,
      });
      
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task AddCharacter(int projectId, string name, bool isPublic, List<int> parentCharacterGroupIds, bool isAcceptingClaims, string description, bool hidePlayerForCharacter)
    {
      var characterGroups = await  ValidateCharacterGroupList(projectId, Required(parentCharacterGroupIds));

      UnitOfWork.GetDbSet<Character>().Add(
        new Character
        {
          CharacterName = Required(name),
          Groups = characterGroups,
          ProjectId = projectId,
          IsPublic = isPublic,
          IsActive = true,
          IsAcceptingClaims = isAcceptingClaims,
          Description = new MarkdownString(description),
          HidePlayerForCharacter = hidePlayerForCharacter
        });
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task EditCharacter(int currentUserId, int characterId, int projectId, string name, bool isPublic,
      List<int> parentCharacterGroupIds, bool isAcceptingClaims, string contents, bool hidePlayerForCharacter,
      IDictionary<int, string> characterFields)
    {
      var character = await LoadProjectSubEntityAsync<Character>(projectId, characterId);
      character.RequestMasterAccess(currentUserId, acl => acl.CanEditRoles);

      character.CharacterName = Required(name);
      character.IsAcceptingClaims = isAcceptingClaims;
      character.IsPublic = isPublic;
      character.Description = new MarkdownString(contents);
      character.HidePlayerForCharacter = hidePlayerForCharacter;
      character.Groups.AssignLinksList(await ValidateCharacterGroupList(projectId, Required(parentCharacterGroupIds)));
      SaveCharacterFieldsImpl(currentUserId, character, characterFields);

      await UnitOfWork.SaveChangesAsync();
    }

    public async Task MoveCharacterGroup(int currentUserId, int projectId, int charactergroupId, int parentCharacterGroupId,
      int direction)
    {
      var parentCharacterGroup = await ProjectRepository.LoadGroupWithChildsAsync(projectId, parentCharacterGroupId);
      parentCharacterGroup.RequestMasterAccess(currentUserId, acl => acl.CanEditRoles);

      var thisCharacterGroup = parentCharacterGroup.ChildGroups.Single(i => i.CharacterGroupId == charactergroupId);

      var voc = parentCharacterGroup.GetCharacterGroupsContainer();
      switch (direction)
      {
        case -1:
          voc.MoveUp(thisCharacterGroup);
          break;
        case 1:
          voc.MoveDown(thisCharacterGroup);
          break;
        default:
          throw new ArgumentException(nameof(direction));
      }

      parentCharacterGroup.ChildGroupsOrdering = voc.GetStoredOrder();
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task MoveCharacter(int currentUserId, int projectId, int characterId, int parentCharacterGroupId, int direction)
    {
      var parentCharacterGroup = await ProjectRepository.LoadGroupWithChildsAsync(projectId, parentCharacterGroupId);
      parentCharacterGroup.RequestMasterAccess(currentUserId, acl => acl.CanEditRoles);

      var thisCharacterGroup = parentCharacterGroup.Characters.Single(i => i.CharacterId == characterId);

      var voc = parentCharacterGroup.GetCharactersContainer();
      switch (direction)
      {
        case -1:
        voc.MoveUp(thisCharacterGroup);
        break;
        case 1:
        voc.MoveDown(thisCharacterGroup);
        break;
        default:
        throw new ArgumentException(nameof(direction));
      }

      parentCharacterGroup.ChildCharactersOrdering = voc.GetStoredOrder();
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task EditCharacterGroup(int projectId, int characterGroupId, string name, bool isPublic,
      List<int> parentCharacterGroupIds, string description, bool haveDirectSlots, int directSlots,
      int? responsibleMasterId)
    {
      var characterGroup = await LoadProjectSubEntityAsync<CharacterGroup>(projectId, characterGroupId);
      if (!characterGroup.IsRoot) //We shoud not edit root group, except of possibility of direct claims here
      {
        characterGroup.CharacterGroupName = Required(name);
        characterGroup.IsPublic = isPublic;
        var characterGroupIds = Required(parentCharacterGroupIds);
        characterGroup.ParentGroups.AssignLinksList(await ValidateCharacterGroupList(projectId, characterGroupIds));
        characterGroup.Description = new MarkdownString(description);
      }
      if (responsibleMasterId != null &&
          characterGroup.Project.ProjectAcls.All(acl => acl.UserId != responsibleMasterId))
      {
        throw new Exception("No such master");
      }
      characterGroup.ResponsibleMasterUserId = responsibleMasterId;
      characterGroup.AvaiableDirectSlots = directSlots;
      characterGroup.HaveDirectSlots = haveDirectSlots;
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task DeleteCharacterGroup(int projectId, int characterGroupId)
    {
      var characterGroup = await ProjectRepository.LoadGroupAsync(projectId, characterGroupId);
      if (characterGroup == null || characterGroup.ProjectId != projectId) throw new DbEntityValidationException();

      if (characterGroup.HasActiveClaims())
      {
        throw new DbEntityValidationException();
      }
      
      ReparentChilds(characterGroup, characterGroup.ChildGroups);
      ReparentChilds(characterGroup, characterGroup.Characters);
      if (characterGroup.CanBePermanentlyDeleted)
      {
        characterGroup.DirectlyRelatedPlotFolders.CleanLinksList();
        characterGroup.DirectlyRelatedPlotElements.CleanLinksList();
        characterGroup.ParentGroups.CleanLinksList();
      }
      SmartDelete(characterGroup);
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task EditProject(int projectId, string projectName, string claimApplyRules, string projectAnnounce)
    {
      var project = await UnitOfWork.GetDbSet<Project>().Include(p =>p.Details).SingleOrDefaultAsync(p => p.ProjectId == projectId);
      project.Details = project.Details ?? new ProjectDetails {ProjectId = projectId};
      project.Details.ClaimApplyRules = new MarkdownString(claimApplyRules);
      project.Details.ProjectAnnounce = new MarkdownString(projectAnnounce);
      project.ProjectName = Required(projectName);
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task GrantAccess(int projectId, int currentUserId, int userId, bool canGrantRights, bool canChangeFields, bool canChangeProjectProperties, bool canApproveClaims, bool canEditRoles, bool canAcceptCash, bool canManageMoney)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      project.RequestMasterAccess(currentUserId, a => a.CanGrantRights);

      var acl = project.ProjectAcls.SingleOrDefault(a => a.UserId == userId);
      if (acl == null)
      {
        acl = new ProjectAcl() {ProjectId = project.ProjectId, UserId = userId};
        project.ProjectAcls.Add(acl);
      }
      acl.CanGrantRights = canGrantRights;
      acl.CanChangeFields = canChangeFields;
      acl.CanChangeProjectProperties = canChangeProjectProperties;
      acl.CanApproveClaims = canApproveClaims;
      acl.CanEditRoles = canEditRoles;
      acl.CanAcceptCash = canAcceptCash;
      acl.CanManageMoney = canManageMoney;

      await UnitOfWork.SaveChangesAsync();
    }


    public async Task RemoveAccess(int projectId, int currentUserId, int userId)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      project.RequestMasterAccess(currentUserId, a => a.CanGrantRights);

      var acl = project.ProjectAcls.Single(a => a.ProjectId == projectId && a.UserId == userId);
      UnitOfWork.GetDbSet<ProjectAcl>().Remove(acl);
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task ChangeAccess(int projectId, int currentUserId, int userId, bool canGrantRights, bool canChangeFields, bool canChangeProjectProperties, bool canApproveClaims, bool canEditRoles, bool canAcceptCash, bool canManageMoney)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      project.RequestMasterAccess(currentUserId, a => a.CanGrantRights);
      
      var acl = project.ProjectAcls.Single(a => a.ProjectId == projectId && a.UserId == userId);
      acl.CanGrantRights = canGrantRights;
      acl.CanChangeFields = canChangeFields;
      acl.CanChangeProjectProperties = canChangeProjectProperties;
      acl.CanApproveClaims = canApproveClaims;
      acl.CanEditRoles = canEditRoles;
      acl.CanAcceptCash = canAcceptCash;
      acl.CanManageMoney = canManageMoney;

      await UnitOfWork.SaveChangesAsync();
    }

    public async Task UpdateSubscribeForGroup(int projectId, int characterGroupId, int currentUserId, bool claimStatusChangeValue, bool commentsValue, bool fieldChangeValue)
    {
      var group = await ProjectRepository.LoadGroupAsync(projectId, characterGroupId);
      group.RequestMasterAccess(currentUserId);
      
      var needSubscrive = claimStatusChangeValue || commentsValue || fieldChangeValue;
      var user = await UserRepository.GetWithSubscribe(currentUserId);
      var direct = user.Subscriptions.SingleOrDefault(s => s.CharacterGroupId == characterGroupId);
      if (needSubscrive)
      {
        if (direct == null)
        {
          direct = new UserSubscription()
          {
            UserId = currentUserId,
            CharacterGroupId = characterGroupId,
            ProjectId = projectId
          };
          user.Subscriptions.Add(direct);
        }
        direct.ClaimStatusChange = claimStatusChangeValue;
        direct.Comments = commentsValue;
        direct.FieldChange = fieldChangeValue;
      }
      else
      {
        if (direct != null)
        {
          UnitOfWork.GetDbSet<UserSubscription>().Remove(direct);
        }
      }
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task DeleteCharacter(int projectId, int characterId)
    {
      var character = await ProjectRepository.GetCharacterAsync(projectId, characterId);
      if (character == null || character.ProjectId != projectId) throw new DbEntityValidationException();

      if (character.HasActiveClaims())
      {
        throw new DbEntityValidationException();
      }

      if (character.CanBePermanentlyDeleted)
      {
        character.DirectlyRelatedPlotElements.CleanLinksList();
        character.Groups.CleanLinksList();
      }
      SmartDelete(character);
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task CreateFieldValue(int projectId, int projectCharacterFieldId, int currentUserId, string label, string description)
    {
      var field = await ProjectRepository.GetProjectField(projectId, projectCharacterFieldId);

      field.RequestMasterAccess(currentUserId, acl => acl.CanChangeFields);

      field.DropdownValues.Add(new ProjectCharacterFieldDropdownValue()
      {
        Description = description,
        Label = label,
        IsActive = true,
        WasEverUsed = false,
        ProjectId = field.ProjectId,
        ProjectCharacterFieldId = field.ProjectCharacterFieldId
      }
        );

      await UnitOfWork.SaveChangesAsync();
    }

    public async Task UpdateFieldValue(int projectId, int projectCharacterFieldDropdownValueId, int currentUserId, string label,
      string description)
    {
      var field = await ProjectRepository.GetFieldValue(projectId, projectCharacterFieldDropdownValueId);

      field.RequestMasterAccess(currentUserId, acl => acl.CanChangeFields);

      field.Description = description;
      field.Label = label;
      field.IsActive = true;
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task DeleteFieldValue(int projectId, int projectCharacterFieldDropdownValueId, int currentUserId)
    {
      var field = await ProjectRepository.GetFieldValue(projectId, projectCharacterFieldDropdownValueId);

      field.RequestMasterAccess(currentUserId, acl => acl.CanChangeFields);

      SmartDelete(field);
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task SaveCharacterFields(int projectId, int characterId, int currentUserId, IDictionary<int, string> newFieldValue)
    {
      //TODO: Prevent lazy load here - use repository 
      var character = await LoadProjectSubEntityAsync<Character>(projectId, characterId);

      SaveCharacterFieldsImpl(currentUserId, character, newFieldValue);
      await UnitOfWork.SaveChangesAsync();
    }

    private static void SaveCharacterFieldsImpl(int currentUserId, Character character, IDictionary<int, string> newFieldValue)
    {
      var fields = character.Fields();

      var hasMasterAccess = character.HasMasterAccess(currentUserId);
      var hasPlayerAccess = character.ApprovedClaim?.PlayerUserId == currentUserId;

      if (!hasMasterAccess && !hasPlayerAccess)
      {
        throw new DbEntityValidationException();
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

        field.Value = newValue;

        if (!field.Field.WasEverUsed)
        {
          field.Field.WasEverUsed = true;
        }

        if (field.Field.HasValueList())
        {
          var value = field.GetDropdownValueOrDefault();

          if (value == null)
          {
            throw new DbEntityValidationException();
          }

          if (!value.WasEverUsed)
          {
            value.WasEverUsed = true;
          }
        }
      }
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
