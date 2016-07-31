using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Linq.Expressions;
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
        IsAcceptingClaims = false,
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
            ResponsibleMasterUserId = creator.UserId,
            HaveDirectSlots = true,
            AvaiableDirectSlots = -1
          }
        },
        ProjectAcls = new List<ProjectAcl>()
        {
          ProjectAcl.CreateRootAcl(creator.UserId)
        }
      };
      project.MarkTreeModified();
      UnitOfWork.GetDbSet<Project>().Add(project);
      await UnitOfWork.SaveChangesAsync();
      return project;
    }



    public async Task AddCharacterGroup(int projectId, string name, bool isPublic, IReadOnlyCollection<int> parentCharacterGroupIds, string description, bool haveDirectSlotsForSave, int directSlotsForSave, int? responsibleMasterId)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);

      if (responsibleMasterId != null &&
          project.ProjectAcls.All(acl => acl.UserId != responsibleMasterId))
      {
        //TODO: Move this check into ChGroup validation
        throw new Exception("No such master");
      }

      UnitOfWork.GetDbSet<CharacterGroup>().Add(new CharacterGroup()
      {
        AvaiableDirectSlots = directSlotsForSave,
        HaveDirectSlots = haveDirectSlotsForSave,
        CharacterGroupName = Required(name),
        ParentCharacterGroupIds = await ValidateCharacterGroupList(projectId, Required(() => parentCharacterGroupIds)),
        ProjectId = projectId,
        IsRoot = false,
        IsSpecial = false,
        IsPublic = isPublic,
        IsActive = true,
        Description = new MarkdownString(description),
        ResponsibleMasterUserId = responsibleMasterId,
      });
      project.MarkTreeModified();

      await UnitOfWork.SaveChangesAsync();
    }

    public async Task AddCharacter(int projectId, string name, bool isPublic,
      IReadOnlyCollection<int> parentCharacterGroupIds, bool isAcceptingClaims, string description,
      bool hidePlayerForCharacter, bool isHot)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);

      UnitOfWork.GetDbSet<Character>().Add(
        new Character
        {
          CharacterName = Required(name),
          ParentCharacterGroupIds = await ValidateCharacterGroupList(projectId, Required(parentCharacterGroupIds)),
          ProjectId = projectId,
          IsPublic = isPublic,
          IsActive = true,
          IsAcceptingClaims = isAcceptingClaims,
          Description = new MarkdownString(description),
          HidePlayerForCharacter = hidePlayerForCharacter,
          IsHot = isHot
        });

      project.MarkTreeModified();
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task EditCharacter(int currentUserId, int characterId, int projectId, string name, bool isPublic,
      IReadOnlyCollection<int> parentCharacterGroupIds, bool isAcceptingClaims, string contents,
      bool hidePlayerForCharacter, IDictionary<int, string> characterFields, bool isHot)
    {
      var character = await LoadProjectSubEntityAsync<Character>(projectId, characterId);
      character.RequestMasterAccess(currentUserId, acl => acl.CanEditRoles);

      character.CharacterName = Required(name);
      character.IsAcceptingClaims = isAcceptingClaims;
      character.IsPublic = isPublic;
      character.Description = new MarkdownString(contents);
      character.HidePlayerForCharacter = hidePlayerForCharacter;
      character.IsHot = isHot;
      character.IsActive = true;

      var characterGroups = await ValidateCharacterGroupList(projectId, Required(parentCharacterGroupIds), ensureNotSpecial: true);
      var specialGroupIds = FieldSaveHelper.SaveCharacterFieldsImpl(currentUserId, character, character.ApprovedClaim, characterFields);
      character.ParentCharacterGroupIds =  characterGroups.Union(await ValidateCharacterGroupList(projectId, specialGroupIds)).ToArray();
      character.Project.MarkTreeModified(); //TODO: Can be smarter

      await UnitOfWork.SaveChangesAsync();
    }

    public async Task MoveCharacterGroup(int currentUserId, int projectId, int charactergroupId, int parentCharacterGroupId, short direction)
    {
      var parentCharacterGroup = await ProjectRepository.LoadGroupWithChildsAsync(projectId, parentCharacterGroupId);
      parentCharacterGroup.RequestMasterAccess(currentUserId, acl => acl.CanEditRoles);

      var thisCharacterGroup = parentCharacterGroup.ChildGroups.Single(i => i.CharacterGroupId == charactergroupId);

      parentCharacterGroup.ChildGroupsOrdering = parentCharacterGroup.GetCharacterGroupsContainer().Move(thisCharacterGroup, direction).GetStoredOrder();
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task MoveCharacter(int currentUserId, int projectId, int characterId, int parentCharacterGroupId, short direction)
    {
      var parentCharacterGroup = await ProjectRepository.LoadGroupWithChildsAsync(projectId, parentCharacterGroupId);
      parentCharacterGroup.RequestMasterAccess(currentUserId, acl => acl.CanEditRoles);

      var item = parentCharacterGroup.Characters.Single(i => i.CharacterId == characterId);

      parentCharacterGroup.ChildCharactersOrdering = parentCharacterGroup.GetCharactersContainer().Move(item, direction).GetStoredOrder();
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task CloseProject(int projectId, int currentUserId, bool publishPlot)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);

      var user = await UserRepository.GetById(currentUserId);
      RequestProjectAdminAccess(project, user);
      project.Details = project.Details ?? new ProjectDetails();

      project.Active = false;
      project.IsAcceptingClaims = false;
      project.Details.PublishPlot = publishPlot;
      
      await UnitOfWork.SaveChangesAsync();
    }

    private static void RequestProjectAdminAccess(Project project, User user)
    {
      if (project == null)
      {
        throw new ArgumentNullException(nameof(project));
      }
      if (!project.HasMasterAccess(user.UserId, acl => acl.CanChangeProjectProperties) && user.Auth?.IsAdmin != true)
      {
        throw new NoAccessToProjectException(project, user.UserId, acl => acl.CanChangeProjectProperties);
      }
    }

    public async Task EditCharacterGroup(int projectId, int characterGroupId, string name, bool isPublic,
      IReadOnlyCollection<int> parentCharacterGroupIds, string description, bool haveDirectSlots, int directSlots,
      int? responsibleMasterId)
    {
      var characterGroup = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);
      if (!characterGroup.IsRoot) //We shoud not edit root group, except of possibility of direct claims here
      {
        characterGroup.CharacterGroupName = Required(name);
        characterGroup.IsPublic = isPublic;
        characterGroup.ParentCharacterGroupIds =  await ValidateCharacterGroupList(projectId, Required(parentCharacterGroupIds), ensureNotSpecial: true);
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

      characterGroup.Project.MarkTreeModified(); // Can be smarted than this
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task DeleteCharacterGroup(int projectId, int characterGroupId)
    {
      var characterGroup = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);

      if (characterGroup == null) throw new DbEntityValidationException();

      if (characterGroup.HasActiveClaims())
      {
        throw new DbEntityValidationException();
      }

      characterGroup.Project.MarkTreeModified();

      if (characterGroup.CanBePermanentlyDeleted)
      {
        characterGroup.DirectlyRelatedPlotFolders.CleanLinksList();
        characterGroup.DirectlyRelatedPlotElements.CleanLinksList();
      }
      SmartDelete(characterGroup);
      
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task EditProject(int projectId, int currentUserId, string projectName, string claimApplyRules, string projectAnnounce, bool isAcceptingClaims, bool multipleCharacters, bool publishPlot)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId); 
      project.RequestMasterAccess(currentUserId, acl => acl.CanChangeProjectProperties);

      project.Details = project.Details ?? new ProjectDetails {ProjectId = projectId};
      project.Details.ClaimApplyRules = new MarkdownString(claimApplyRules);
      project.Details.ProjectAnnounce = new MarkdownString(projectAnnounce);
      project.Details.EnableManyCharacters = multipleCharacters;
      project.Details.PublishPlot = publishPlot && !project.Active;
      project.ProjectName = Required(projectName);
      project.IsAcceptingClaims = isAcceptingClaims && project.Active;
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task GrantAccess(int projectId, int currentUserId, int userId, bool canGrantRights,
      bool canChangeFields, bool canChangeProjectProperties, bool canApproveClaims, bool canEditRoles,
      bool canManageMoney, bool canSendMassMails, bool canManagePlots)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      project.RequestMasterAccess(currentUserId, a => a.CanGrantRights);

      var acl = project.ProjectAcls.SingleOrDefault(a => a.UserId == userId);
      if (acl == null)
      {
        acl = new ProjectAcl()
        {
          ProjectId = project.ProjectId,
          UserId = userId,
          Project = project //Used inside UpdatePaymentTypes()
        };
        project.ProjectAcls.Add(acl);
      }
      acl.CanGrantRights = canGrantRights;
      acl.CanChangeFields = canChangeFields;
      acl.CanChangeProjectProperties = canChangeProjectProperties;
      acl.CanManageClaims = canApproveClaims;
      acl.CanEditRoles = canEditRoles;
      acl.CanManageMoney = canManageMoney;
      acl.CanSendMassMails = canSendMassMails;
      acl.CanManagePlots = canManagePlots;

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

    public async Task ChangeAccess(int projectId, int currentUserId, int userId, bool canGrantRights,
      bool canChangeFields, bool canChangeProjectProperties, bool canApproveClaims, bool canEditRoles,
      bool canManageMoney, bool canSendMassMails, bool canManagePlots)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      project.RequestMasterAccess(currentUserId, a => a.CanGrantRights);

      var acl = project.ProjectAcls.Single(a => a.ProjectId == projectId && a.UserId == userId);
      acl.CanGrantRights = canGrantRights;
      acl.CanChangeFields = canChangeFields;
      acl.CanChangeProjectProperties = canChangeProjectProperties;
      acl.CanManageClaims = canApproveClaims;
      acl.CanEditRoles = canEditRoles;
      acl.CanManageMoney = canManageMoney;
      acl.CanSendMassMails = canSendMassMails;
      acl.CanManagePlots = canManagePlots;

      await UnitOfWork.SaveChangesAsync();
    }

    public async Task UpdateSubscribeForGroup(int projectId, int characterGroupId, int currentUserId, bool claimStatusChangeValue, bool commentsValue, bool fieldChangeValue, bool moneyOperationValue)
    {
      var group = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);
      group.RequestMasterAccess(currentUserId);
      
      var needSubscrive = claimStatusChangeValue || commentsValue || fieldChangeValue || moneyOperationValue;
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
        direct.MoneyOperation = moneyOperationValue;
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
      if (character == null) throw new DbEntityValidationException();

      if (character.HasActiveClaims())
      {
        throw new DbEntityValidationException();
      }

      character.Project.MarkTreeModified();

      if (character.CanBePermanentlyDeleted)
      {
        character.DirectlyRelatedPlotElements.CleanLinksList();
      }
      character.IsActive = false;
      await UnitOfWork.SaveChangesAsync();
    }
  }
}
