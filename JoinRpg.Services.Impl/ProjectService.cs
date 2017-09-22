using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Security.Permissions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
  [UsedImplicitly]
  internal class ProjectService : DbServiceImplBase, IProjectService
  {
    private IEmailService EmailService { get; }
    private IFieldDefaultValueGenerator FieldDefaultValueGenerator { get; }

    public ProjectService(IUnitOfWork unitOfWork, IEmailService emailService, IFieldDefaultValueGenerator fieldDefaultValueGenerator) : base(unitOfWork)
    {
      FieldDefaultValueGenerator = fieldDefaultValueGenerator;
      EmailService = emailService;
    }

    public async Task<Project> AddProject(string projectName, User creator)
    {
      var rootGroup = new CharacterGroup()
      {
        IsPublic = true,
        IsRoot = true,
        //TODO[Localize]
        CharacterGroupName = "Все роли",
        IsActive = true,
        ResponsibleMasterUserId = creator.UserId,
        HaveDirectSlots = true,
        AvaiableDirectSlots = -1,
      };
      MarkCreatedNow(rootGroup);
      var project = new Project()
      {
        Active = true,
        IsAcceptingClaims = false,
        CreatedDate = Now,
        ProjectName = Required(projectName),
        CharacterGroups = new List<CharacterGroup>()
        {
          rootGroup
        },
        ProjectAcls = new List<ProjectAcl>()
        {
          ProjectAcl.CreateRootAcl(creator.UserId, isOwner: true)
        },
        Details = new ProjectDetails()
      };
      MarkTreeModified(project);
      UnitOfWork.GetDbSet<Project>().Add(project);
      await UnitOfWork.SaveChangesAsync();
      return project;
    }


    public async Task AddCharacterGroup(int projectId, string name, bool isPublic,
      IReadOnlyCollection<int> parentCharacterGroupIds, string description,
      bool haveDirectSlotsForSave, int directSlotsForSave, int? responsibleMasterId)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);

      if (responsibleMasterId != null &&
          project.ProjectAcls.All(acl => acl.UserId != responsibleMasterId))
      {
        //TODO: Move this check into ChGroup validation
        throw new Exception("No such master");
      }

      project.RequestMasterAccess(CurrentUserId, acl => acl.CanEditRoles);
      project.EnsureProjectActive();

      Create(new CharacterGroup()
      {
        AvaiableDirectSlots = directSlotsForSave,
        HaveDirectSlots = haveDirectSlotsForSave,
        CharacterGroupName = Required(name),
        ParentCharacterGroupIds =
          await ValidateCharacterGroupList(projectId, Required(() => parentCharacterGroupIds)),
        ProjectId = projectId,
        IsRoot = false,
        IsSpecial = false,
        IsPublic = isPublic,
        IsActive = true,
        Description = new MarkdownString(description),
        ResponsibleMasterUserId = responsibleMasterId,
      });

      MarkTreeModified(project);

      await UnitOfWork.SaveChangesAsync();
    }

    public async Task AddCharacter(int projectId, int currentUserId, string name, bool isPublic,
      IReadOnlyCollection<int> parentCharacterGroupIds, bool isAcceptingClaims, string description,
      bool hidePlayerForCharacter, bool isHot)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);

      project.RequestMasterAccess(currentUserId, acl => acl.CanEditRoles);
      project.EnsureProjectActive();

      var character = new Character
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
      };
      Create(character);
      MarkTreeModified(project);

      await UnitOfWork.SaveChangesAsync();
    }

    //TODO: move character operations to a separate service.
    public async Task EditCharacter(int currentUserId, int characterId, int projectId, string name, bool isPublic,
      IReadOnlyCollection<int> parentCharacterGroupIds, bool isAcceptingClaims, string contents,
      bool hidePlayerForCharacter, IDictionary<int, string> characterFields, bool isHot)
    {
      var character = await LoadProjectSubEntityAsync<Character>(projectId, characterId);
      character.RequestMasterAccess(currentUserId, acl => acl.CanEditRoles);

      character.EnsureProjectActive();

      var changedAttributes = new Dictionary<string, PreviousAndNewValue>();

      changedAttributes.Add("Имя персонажа", new PreviousAndNewValue(name, character.CharacterName.Trim()));
      character.CharacterName = name.Trim();
      
      character.IsAcceptingClaims = isAcceptingClaims;
      character.IsPublic = isPublic;

      var newDescription = new MarkdownString(contents);

      changedAttributes.Add("Описание персонажа", new PreviousAndNewValue(newDescription, character.Description));
      character.Description = newDescription;

      character.HidePlayerForCharacter = hidePlayerForCharacter;
      character.IsHot = isHot;
      character.IsActive = true;

      character.ParentCharacterGroupIds = await ValidateCharacterGroupList(projectId,
        Required(parentCharacterGroupIds), ensureNotSpecial: true);
      var changedFields = FieldSaveHelper.SaveCharacterFields(currentUserId, character, characterFields, FieldDefaultValueGenerator);

      MarkChanged(character);
      MarkTreeModified(character.Project); //TODO: Can be smarter

      FieldsChangedEmail email = null;
      changedAttributes = changedAttributes
        .Where(attr => attr.Value.DisplayString != attr.Value.PreviousDisplayString)
        .ToDictionary(x => x.Key, x => x.Value);

      if (changedFields.Any() || changedAttributes.Any())
      {
        var user = await UserRepository.GetById(currentUserId);
        email = EmailHelpers.CreateFieldsEmail(
          character,
          s => s.FieldChange,
          user,
          changedFields,
          changedAttributes);
      }
      await UnitOfWork.SaveChangesAsync();

      if (email != null)
      {
        await EmailService.Email(email);
      }
    }

    public async Task MoveCharacterGroup(int currentUserId, int projectId, int charactergroupId, int parentCharacterGroupId, short direction)
    {
      var parentCharacterGroup = await ProjectRepository.LoadGroupWithChildsAsync(projectId, parentCharacterGroupId);
      parentCharacterGroup.RequestMasterAccess(currentUserId, acl => acl.CanEditRoles);
      parentCharacterGroup.EnsureProjectActive();

      var thisCharacterGroup = parentCharacterGroup.ChildGroups.Single(i => i.CharacterGroupId == charactergroupId);

      parentCharacterGroup.ChildGroupsOrdering = parentCharacterGroup.GetCharacterGroupsContainer().Move(thisCharacterGroup, direction).GetStoredOrder();
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task MoveCharacter(int currentUserId, int projectId, int characterId, int parentCharacterGroupId, short direction)
    {
      var parentCharacterGroup = await ProjectRepository.LoadGroupWithChildsAsync(projectId, parentCharacterGroupId);
      parentCharacterGroup.RequestMasterAccess(currentUserId, acl => acl.CanEditRoles);
      parentCharacterGroup.EnsureProjectActive();

      var item = parentCharacterGroup.Characters.Single(i => i.CharacterId == characterId);

      parentCharacterGroup.ChildCharactersOrdering = parentCharacterGroup.GetCharactersContainer().Move(item, direction).GetStoredOrder();
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task CloseProject(int projectId, int currentUserId, bool publishPlot)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);

      var user = await UserRepository.GetById(currentUserId);
      RequestProjectAdminAccess(project, user);

      project.Active = false;
      project.IsAcceptingClaims = false;
      project.Details.PublishPlot = publishPlot;
      
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task SetCheckInOptions(int projectId, bool checkInProgress, bool enableCheckInModule,
      bool modelAllowSecondRoles)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      project.RequestMasterAccess(CurrentUserId, acl => acl.CanChangeProjectProperties);

      project.Details.CheckInProgress = checkInProgress && enableCheckInModule;
      project.Details.EnableCheckInModule = enableCheckInModule;
      project.Details.AllowSecondRoles = modelAllowSecondRoles && enableCheckInModule;
      await UnitOfWork.SaveChangesAsync();
    }

      [PrincipalPermission(SecurityAction.Demand, Role = Security.AdminRoleName)]
      public async Task GrantAccessAsAdmin(int projectId)
      {
          var project = await ProjectRepository.GetProjectAsync(projectId);

          var acl = project.ProjectAcls.SingleOrDefault(a => a.UserId == CurrentUserId);
          if (acl == null)
          {
              project.ProjectAcls.Add(ProjectAcl.CreateRootAcl(CurrentUserId));
          }

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

    public async Task EditCharacterGroup(int projectId, int currentUserId, int characterGroupId, string name,
      bool isPublic, IReadOnlyCollection<int> parentCharacterGroupIds, string description, bool haveDirectSlots,
      int directSlots, int? responsibleMasterId)
    {
      var characterGroup = await ProjectRepository.GetGroupAsync(projectId, characterGroupId);

      characterGroup.RequestMasterAccess(currentUserId, acl => acl.CanEditRoles);
      characterGroup.EnsureProjectActive();

      if (!characterGroup.IsRoot) //We shoud not edit root group, except of possibility of direct claims here
      {
        characterGroup.CharacterGroupName = Required(name);
        characterGroup.IsPublic = isPublic;
        characterGroup.ParentCharacterGroupIds =
          await ValidateCharacterGroupList(projectId, Required(parentCharacterGroupIds), ensureNotSpecial: true);
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

      MarkTreeModified(characterGroup.Project); // Can be smarted than this
      MarkChanged(characterGroup);
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

      characterGroup.RequestMasterAccess(CurrentUserId, acl => acl.CanEditRoles);
      characterGroup.EnsureProjectActive();


      MarkTreeModified(characterGroup.Project);
      MarkChanged(characterGroup);

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
      if (!project.HasMasterAccess(currentUserId, a => a.CanGrantRights))
      {
        var user = await UserRepository.GetById(currentUserId);
        if (!user.Auth?.IsAdmin == true)
        {
          project.RequestMasterAccess(currentUserId, a => a.CanGrantRights);
        }
      }
      project.EnsureProjectActive();

      var acl = project.ProjectAcls.SingleOrDefault(a => a.UserId == userId);
      if (acl == null)
      {
        acl = new ProjectAcl
        {
          ProjectId = project.ProjectId,
          UserId = userId,
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

    public async Task RemoveAccess(int projectId, int userId, int? newResponsibleMasterId)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      project.RequestMasterAccess(CurrentUserId, a => a.CanGrantRights);

      if (!project.ProjectAcls.Any(a => a.CanGrantRights && a.UserId != userId))
      {
        throw new DbEntityValidationException();
      }

      var acl = project.ProjectAcls.Single(a => a.ProjectId == projectId && a.UserId == userId);

      var respFor = await ProjectRepository.GetGroupsWithResponsible(projectId);
      if (respFor.Any(item => item.ResponsibleMasterUserId == userId))
      {
        throw new MasterHasResponsibleException(acl);
      }

      var claims = await ClaimsRepository.GetClaimsForMaster(projectId, acl.UserId, ClaimStatusSpec.Any);

      if (claims.Any())
      {
        if (newResponsibleMasterId == null)
        {
          throw new MasterHasResponsibleException(acl);
        }

        project.RequestMasterAccess((int) newResponsibleMasterId);

        foreach (var claim in claims)
        {
          claim.ResponsibleMasterUserId = newResponsibleMasterId;
        }
      }

      if (acl.IsOwner)
      {
        if (acl.UserId == CurrentUserId)
        {
          // if owner removing himself, assign "random" owner
          project.ProjectAcls.OrderBy(a => a.UserId).First().IsOwner = true;
        }
        else
        {
          //who kills the king, becomes one
          project.ProjectAcls.Single(a => a.UserId == CurrentUserId).IsOwner = true;
        }
      }

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
      group.EnsureProjectActive();
      
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

    public async Task DeleteCharacter(int projectId, int characterId, int currentUserId)
    {
      var character = await CharactersRepository.GetCharacterAsync(projectId, characterId);

      character.RequestMasterAccess(currentUserId, acl => acl.CanEditRoles);
      character.EnsureProjectActive();

      if (character.HasActiveClaims())
      {
        throw new DbEntityValidationException();
      }

      MarkTreeModified(character.Project);

      if (character.CanBePermanentlyDeleted)
      {
        character.DirectlyRelatedPlotElements.CleanLinksList();
      }
      character.IsActive = false;
      MarkChanged(character);
      await UnitOfWork.SaveChangesAsync();
    }
  }
}
