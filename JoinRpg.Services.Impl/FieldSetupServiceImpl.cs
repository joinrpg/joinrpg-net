using System.Collections.Generic;
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
  public class FieldSetupServiceImpl : DbServiceImplBase, IFieldSetupService
  {
    public async Task AddField(int projectId, int currentUserId, ProjectFieldType fieldType, string name, string fieldHint, bool canPlayerEdit, bool canPlayerView, bool isPublic, FieldBoundTo fieldBoundTo)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);

      project.RequestMasterAccess(currentUserId, acl => acl.CanChangeFields);

      var field = new ProjectField
      {
        FieldName = Required(name),
        Description = new MarkdownString(fieldHint),
        CanPlayerEdit = canPlayerEdit,
        CanPlayerView = canPlayerView,
        IsPublic = isPublic,
        ProjectId = projectId,
        Project = project, //We require it for CreateOrUpdateSpecailGroup
        FieldType = fieldType,
        FieldBoundTo = fieldBoundTo,
        IsActive = true
      };

      CreateOrUpdateSpecialGroup(field);

      UnitOfWork.GetDbSet<ProjectField>().Add(field);
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task UpdateFieldParams(int? currentUserId, int projectId, int fieldId, string name, string fieldHint, bool canPlayerEdit, bool canPlayerView, bool isPublic)
    {
      var field = await UnitOfWork.GetDbSet<ProjectField>().FindAsync(fieldId);
      if (field == null || field.ProjectId != projectId) throw new DbEntityValidationException();

      field.RequestMasterAccess(currentUserId, acl => acl.CanChangeFields);

      field.FieldName = Required(name);
      field.Description.Contents = fieldHint;
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
      var field = await UnitOfWork.GetDbSet<ProjectField>().FindAsync(projectCharacterFieldId);

      foreach (var fieldValueVariant in field.DropdownValues)
      {
        DeleteFieldVariantValueImpl(fieldValueVariant);
      }

      var characterGroup = field.CharacterGroup; // SmartDelete will nullify all depend properties
      if (SmartDelete(field))
      {
        SmartDelete(characterGroup);
      }
      else
      {
        characterGroup.IsActive = false;
      }
      await UnitOfWork.SaveChangesAsync();
    }

    public FieldSetupServiceImpl(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }

    public async Task CreateFieldValueVariant(int projectId, int projectCharacterFieldId, int currentUserId, string label, string description)
    {
      var field = await ProjectRepository.GetProjectField(projectId, projectCharacterFieldId);

      field.RequestMasterAccess(currentUserId, acl => acl.CanChangeFields);

      var fieldValue = new ProjectFieldDropdownValue()
      {
        Description = new MarkdownString(description),
        Label = label,
        IsActive = true,
        WasEverUsed = false,
        ProjectId = field.ProjectId,
        ProjectFieldId = field.ProjectFieldId,
        Project = field.Project,
        ProjectField = field
      };

      CreateOrUpdateSpecialGroup(fieldValue);

      field.DropdownValues.Add(fieldValue);

      await UnitOfWork.SaveChangesAsync();
    }

    private static void CreateOrUpdateSpecialGroup(ProjectFieldDropdownValue fieldValue)
    {
      if (!fieldValue.ProjectField.HasSpecialGroup())
      {
        return;
      }
      CreateOrUpdateSpecialGroup(fieldValue.ProjectField);

      fieldValue.CharacterGroup = fieldValue.CharacterGroup ?? new CharacterGroup()
      {
        AvaiableDirectSlots = 0,
        HaveDirectSlots = false,
        ParentGroups = new List<CharacterGroup> { fieldValue.ProjectField.CharacterGroup },
        ProjectId = fieldValue.ProjectId,
        IsRoot = false,
        IsSpecial = true,
        ResponsibleMasterUserId = null,
      };

      fieldValue.CharacterGroup.IsPublic = fieldValue.ProjectField.IsPublic;
      fieldValue.CharacterGroup.IsActive = fieldValue.IsActive;
      fieldValue.CharacterGroup.Description = fieldValue.Description;
      fieldValue.CharacterGroup.CharacterGroupName = fieldValue.GetSpecialGroupName();
    }

    private static void CreateOrUpdateSpecialGroup(ProjectField field)
    {
      if (!field.HasSpecialGroup())
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
      foreach (var fieldValue in field.DropdownValues.Where(dv => dv.CharacterGroup != null))
      {
        fieldValue.CharacterGroup.IsPublic = field.IsPublic;
        fieldValue.CharacterGroup.CharacterGroupName = fieldValue.GetSpecialGroupName();
      }
      field.CharacterGroup.IsActive = field.IsActive;
      field.CharacterGroup.Description = field.Description;

      field.CharacterGroup.CharacterGroupName = field.GetSpecialGroupName();
    }

    public async Task UpdateFieldValueVariant(int projectId, int projectFieldDropdownValueId, int currentUserId, string label, string description, int projectFieldId)
    {
      var field = await ProjectRepository.GetFieldValue(projectId, projectFieldId, projectFieldDropdownValueId);

      field.RequestMasterAccess(currentUserId, acl => acl.CanChangeFields);

      field.Description = new MarkdownString(description);
      field.Label = label;
      field.IsActive = true;

      CreateOrUpdateSpecialGroup(field);

      await UnitOfWork.SaveChangesAsync();
    }

    public async Task DeleteFieldValueVariant(int projectId, int projectFieldDropdownValueId, int currentUserId, int projectFieldId)
    {
      var field = await ProjectRepository.GetFieldValue(projectId, projectFieldId, projectFieldDropdownValueId);

      field.RequestMasterAccess(currentUserId, acl => acl.CanChangeFields);

      DeleteFieldVariantValueImpl(field);
      await UnitOfWork.SaveChangesAsync();
    }

    private void DeleteFieldVariantValueImpl(ProjectFieldDropdownValue field)
    {
      var characterGroup = field.CharacterGroup; // SmartDelete will nullify all depend properties
      if (SmartDelete(field))
      {
        SmartDelete(characterGroup);
      }
      else
      {
        characterGroup.IsActive = false;
      }
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
