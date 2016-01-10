using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
  [UsedImplicitly]
  public class FieldSetupServiceImpl : DbServiceImplBase, IFieldSetupService
  {
    public async Task AddCharacterField(int projectId, int currentUserId, CharacterFieldType fieldType, string name, string fieldHint,
  bool canPlayerEdit, bool canPlayerView, bool isPublic)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);

      project.RequestMasterAccess(currentUserId, acl => acl.CanChangeFields);

      var field = new ProjectCharacterField
      {
        FieldName = Required(name),
        FieldHint = new MarkdownString(fieldHint),
        CanPlayerEdit = canPlayerEdit,
        CanPlayerView = canPlayerView,
        IsPublic = isPublic,
        ProjectId = projectId,
        Project = project, //We requireit for CreateOrUpdateSpecailGroup
        FieldType = fieldType,
        IsActive = true
      };

      CreateOrUpdateSpecialGroup(field);

      UnitOfWork.GetDbSet<ProjectCharacterField>().Add(field);
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task UpdateCharacterField(int? currentUserId, int projectId, int fieldId, string name, string fieldHint, bool canPlayerEdit, bool canPlayerView, bool isPublic)
    {
      var field = await UnitOfWork.GetDbSet<ProjectCharacterField>().FindAsync(fieldId);
      if (field == null || field.ProjectId != projectId) throw new DbEntityValidationException();

      field.RequestMasterAccess(currentUserId, acl => acl.CanChangeFields);

      field.FieldName = Required(name);
      field.FieldHint.Contents = fieldHint;
      field.CanPlayerEdit = canPlayerEdit;
      field.CanPlayerView = canPlayerView;
      field.IsPublic = isPublic;
      field.IsActive = true;

      CreateOrUpdateSpecialGroup(field);

      await UnitOfWork.SaveChangesAsync();
    }

    //TODO: pass projectId & CurrentUserId
    public async Task DeleteField(int projectCharacterFieldId)
    {
      var field = await UnitOfWork.GetDbSet<ProjectCharacterField>().FindAsync(projectCharacterFieldId);
      var characterGroup = field.CharacterGroup; // SmartDelete will nullify all depend properties
      if (SmartDelete(field))
      {
        SmartDelete(characterGroup);
      }
      await UnitOfWork.SaveChangesAsync();
    }

    public FieldSetupServiceImpl(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }

    public async Task CreateFieldValue(int projectId, int projectCharacterFieldId, int currentUserId, string label, string description)
    {
      var field = await ProjectRepository.GetProjectField(projectId, projectCharacterFieldId);

      field.RequestMasterAccess(currentUserId, acl => acl.CanChangeFields);

      var fieldValue = new ProjectCharacterFieldDropdownValue()
      {
        Description = new MarkdownString(description),
        Label = label,
        IsActive = true,
        WasEverUsed = false,
        ProjectId = field.ProjectId,
        ProjectCharacterFieldId = field.ProjectCharacterFieldId,
        Project = field.Project,
        ProjectCharacterField = field
      };

      CreateOrUpdateSpecialGroup(fieldValue);

      field.DropdownValues.Add(fieldValue);

      await UnitOfWork.SaveChangesAsync();
    }

    private static void CreateOrUpdateSpecialGroup(ProjectCharacterFieldDropdownValue fieldValue)
    {
      CreateOrUpdateSpecialGroup(fieldValue.ProjectCharacterField);

      fieldValue.CharacterGroup = fieldValue.CharacterGroup ?? new CharacterGroup()
      {
        AvaiableDirectSlots = 0,
        HaveDirectSlots = false,
        ParentGroups = new List<CharacterGroup> { fieldValue.ProjectCharacterField.CharacterGroup },
        ProjectId = fieldValue.ProjectId,
        IsRoot = false,
        IsSpecial = true,
        ResponsibleMasterUserId = null,
      };

      fieldValue.CharacterGroup.IsPublic = fieldValue.ProjectCharacterField.IsPublic;
      fieldValue.CharacterGroup.IsActive = fieldValue.IsActive;
      fieldValue.CharacterGroup.Description = fieldValue.Description;
      fieldValue.CharacterGroup.CharacterGroupName = fieldValue.GetSpecialGroupName();
    }

    private static void CreateOrUpdateSpecialGroup(ProjectCharacterField field)
    {
      if (!field.HasValueList())
      {
        return;
      }

      field.CharacterGroup = field.CharacterGroup ?? new CharacterGroup()
      {
        AvaiableDirectSlots = 0,
        HaveDirectSlots = false,
        ParentGroups = new List<CharacterGroup> { field.Project.RootGroup },
        ProjectId = field.ProjectId,
        IsRoot = false,
        IsSpecial = true,
        ResponsibleMasterUserId = null,
      };

      field.CharacterGroup.IsPublic = field.IsPublic;
      field.CharacterGroup.IsActive = field.IsActive;
      field.CharacterGroup.Description = field.FieldHint;

      field.CharacterGroup.CharacterGroupName = field.GetSpecialGroupName();
    }

    public async Task UpdateFieldValue(int projectId, int projectCharacterFieldDropdownValueId, int currentUserId, string label,
      string description)
    {
      var field = await ProjectRepository.GetFieldValue(projectId, projectCharacterFieldDropdownValueId);

      field.RequestMasterAccess(currentUserId, acl => acl.CanChangeFields);

      field.Description = new MarkdownString(description);
      field.Label = label;
      field.IsActive = true;

      CreateOrUpdateSpecialGroup(field);

      await UnitOfWork.SaveChangesAsync();
    }

    public async Task DeleteFieldValue(int projectId, int projectCharacterFieldDropdownValueId, int currentUserId)
    {
      var field = await ProjectRepository.GetFieldValue(projectId, projectCharacterFieldDropdownValueId);

      field.RequestMasterAccess(currentUserId, acl => acl.CanChangeFields);

      var characterGroup = field.CharacterGroup; // SmartDelete will nullify all depend properties
      if (SmartDelete(field))
      {
        SmartDelete(characterGroup);
      }
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task MoveField(int currentUserId, int projectId, int projectcharacterfieldid, short direction)
    {
      var field = await ProjectRepository.GetProjectField(projectId, projectcharacterfieldid);
      field.RequestMasterAccess(currentUserId, acl => acl.CanChangeFields);

      field.Project.ProjectFieldsOrdering = field.Project.GetFieldsContainer().Move(field, direction).GetStoredOrder();
      await UnitOfWork.SaveChangesAsync();
    }
  }
}
