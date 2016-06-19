using System.Collections.Generic;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
  public interface IFieldSetupService
  {
    Task UpdateFieldParams(int? currentUserId, int projectId, int fieldId, string name, string fieldHint,
      bool canPlayerEdit, bool canPlayerView, bool isPublic, MandatoryStatus mandatoryStatus, List<int> showForGroups,
      bool validForNpc, bool includeInPrint);
    Task DeleteField(int currentUserId, int projectId, int projectFieldId);

    Task AddField(int projectId, int currentUserId, ProjectFieldType fieldType, string name, string fieldHint,
      bool canPlayerEdit, bool canPlayerView, bool isPublic, FieldBoundTo fieldBoundTo, MandatoryStatus mandatoryStatus,
      List<int> showForGroups, bool validForNpc, bool includeInPrint);

    Task CreateFieldValueVariant(int projectId, int projectCharacterFieldId, int currentUserId, string label,
      string description);

    Task UpdateFieldValueVariant(int projectId, int projectFieldDropdownValueId, int currentUserId, string label, string description, int projectFieldId);

    Task DeleteFieldValueVariant(int projectId, int projectFieldDropdownValueId, int currentUserId, int projectFieldId);
    Task MoveField(int currentUserId, int projectid, int projectcharacterfieldid, short direction);
    Task MoveFieldValue(int currentUserId, int projectid, int projectFieldId, int projectFieldVariantId, short direction);
  }
}