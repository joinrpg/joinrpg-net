using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
  public interface IFieldSetupService
  {
    Task UpdateCharacterField(int? currentUserId, int projectId, int fieldId, string name, string fieldHint, bool canPlayerEdit, bool canPlayerView, bool isPublic);
    Task DeleteField(int projectCharacterFieldId);

    Task AddCharacterField(int projectId, int currentUserId, CharacterFieldType fieldType, string name, string fieldHint,
      bool canPlayerEdit, bool canPlayerView, bool isPublic);

    Task CreateFieldValue(int projectId, int projectCharacterFieldId, int currentUserId, string label,
      string description);

    Task UpdateFieldValue(int projectId, int projectCharacterFieldDropdownValueId, int currentUserId, string label,
      string description);

    Task DeleteFieldValue(int projectId, int projectCharacterFieldDropdownValueId, int currentUserId);
    Task MoveField(int currentUserId, int projectid, int projectcharacterfieldid, short direction);
  }
}