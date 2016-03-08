using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
  public interface IFieldSetupService
  {
    Task UpdateFieldParams(int? currentUserId, int projectId, int fieldId, string name, string fieldHint, bool canPlayerEdit, bool canPlayerView, bool isPublic, MandatoryStatus mandatoryStatus);
    Task DeleteField(int projectCharacterFieldId);

    Task AddField(int projectId, int currentUserId, ProjectFieldType fieldType, string name, string fieldHint, bool canPlayerEdit, bool canPlayerView, bool isPublic, FieldBoundTo fieldBoundTo, MandatoryStatus mandatoryStatus);

    Task CreateFieldValueVariant(int projectId, int projectCharacterFieldId, int currentUserId, string label,
      string description);

    Task UpdateFieldValueVariant(int projectId, int projectFieldDropdownValueId, int currentUserId, string label, string description, int projectFieldId);

    Task DeleteFieldValueVariant(int projectId, int projectFieldDropdownValueId, int currentUserId, int projectFieldId);
    Task MoveField(int currentUserId, int projectid, int projectcharacterfieldid, short direction);
  }
}