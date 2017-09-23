using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
    public interface IFieldSetupService
    {
        Task UpdateFieldParams(int projectId, int fieldId, string name, string fieldHint,
            bool canPlayerEdit, bool canPlayerView, bool isPublic, MandatoryStatus mandatoryStatus,
            List<int> showForGroups, bool validForNpc, bool includeInPrint, bool showForUnapprovedClaims,
            int price);

        Task<ProjectField> DeleteField(int projectId, int projectFieldId);

        Task DeleteField(ProjectField field);

        Task AddField(int projectId, ProjectFieldType fieldType, string name, string fieldHint,
            bool canPlayerEdit, bool canPlayerView, bool isPublic, FieldBoundTo fieldBoundTo,
            MandatoryStatus mandatoryStatus, List<int> showForGroups, bool validForNpc,
            bool includeInPrint, bool showForUnapprovedClaims, int price);

        Task CreateFieldValueVariant(int projectId, int projectCharacterFieldId, string label,
            string description, string masterDescription, string programmaticValue, int price);

        Task UpdateFieldValueVariant(int projectId, int projectFieldDropdownValueId, string label,
            string description, int projectFieldId, string masterDescription, string programmaticValue,
            int price);

        Task<ProjectFieldDropdownValue> DeleteFieldValueVariant(int projectId, int projectFieldId, int valueId);

        Task DeleteFieldValueVariant(ProjectFieldDropdownValue value);

        Task MoveField(int projectid, int projectcharacterfieldid, short direction);

        Task MoveFieldVariant(int projectid, int projectFieldId, int projectFieldVariantId, short direction);

        Task CreateFieldValueVariants(int projectId, int projectFieldId, [NotNull] string valuesToAdd);

        Task MoveFieldAfter(int projectId, int projectFieldId, int? afterFieldId);
    }
}