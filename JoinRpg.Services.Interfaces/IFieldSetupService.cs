using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
    public interface IFieldSetupService
    {
        Task UpdateFieldParams(int projectId, int fieldId, string name, string fieldHint, bool canPlayerEdit, bool canPlayerView, bool isPublic, MandatoryStatus mandatoryStatus, List<int> showForGroups, bool validForNpc, bool includeInPrint, bool showForUnapprovedClaims, int price, string masterFieldHint);

        Task<ProjectField> DeleteField(int projectId, int projectFieldId);

        Task DeleteField(ProjectField field);

        Task AddField(int projectId, ProjectFieldType fieldType, string name, string fieldHint, bool canPlayerEdit, bool canPlayerView, bool isPublic, FieldBoundTo fieldBoundTo, MandatoryStatus mandatoryStatus, List<int> showForGroups, bool validForNpc, bool includeInPrint, bool showForUnapprovedClaims, int price, string masterFieldHint);

        Task CreateFieldValueVariant(CreateFieldValueVariantRequest request);

        Task UpdateFieldValueVariant(UpdateFieldValueVariantRequest request);

        Task<ProjectFieldDropdownValue> DeleteFieldValueVariant(int projectId, int projectFieldId, int valueId);

        Task MoveField(int projectid, int projectcharacterfieldid, short direction);

        Task MoveFieldVariant(int projectid, int projectFieldId, int projectFieldVariantId, short direction);

        Task CreateFieldValueVariants(int projectId, int projectFieldId, [NotNull] string valuesToAdd);

        Task MoveFieldAfter(int projectId, int projectFieldId, int? afterFieldId);
    }

    public abstract class FieldValueVariantRequestBase
    {
        public int ProjectId { get; }
        public int ProjectFieldId { get; }
        public string Label { get; }
        public string Description { get; }
        public string MasterDescription { get; }
        public string ProgrammaticValue { get; }
        public int Price { get; }
        public bool PlayerSelectable { get; }

        protected FieldValueVariantRequestBase(int projectId,
            string label,
            string description,
            int projectFieldId,
            string masterDescription,
            string programmaticValue,
            int price,
            bool playerSelectable)
        {
            ProjectId = projectId;
            Label = label;
            Description = description;
            ProjectFieldId = projectFieldId;
            MasterDescription = masterDescription;
            ProgrammaticValue = programmaticValue;
            Price = price;
            PlayerSelectable = playerSelectable;
        }
    }

    public class UpdateFieldValueVariantRequest : FieldValueVariantRequestBase
    {
        public UpdateFieldValueVariantRequest(int projectId,
            int projectFieldDropdownValueId,
            string label,
            string description,
            int projectFieldId,
            string masterDescription,
            string programmaticValue,
            int price,
            bool playerSelectable)
            : base(projectId,
                label,
                description,
                projectFieldId,
                masterDescription,
                programmaticValue,
                price,
                playerSelectable)
        {
            ProjectFieldDropdownValueId = projectFieldDropdownValueId;
        }

        public int ProjectFieldDropdownValueId { get; }
    }

    public class CreateFieldValueVariantRequest : FieldValueVariantRequestBase
    {
        public CreateFieldValueVariantRequest(int projectId,
            string label,
            string description,
            int projectFieldId,
            string masterDescription,
            string programmaticValue,
            int price,
            bool playerSelectable)
            : base(projectId,
                label,
                description,
                projectFieldId,
                masterDescription,
                programmaticValue,
                price,
                playerSelectable)
        {
        }
    }
}
