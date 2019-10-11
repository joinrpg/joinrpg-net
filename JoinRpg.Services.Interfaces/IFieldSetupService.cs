using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain.Schedules;

namespace JoinRpg.Services.Interfaces
{
    public interface IFieldSetupService
    {
        Task UpdateFieldParams(UpdateFieldRequest request);

        Task<ProjectField> DeleteField(int projectId, int projectFieldId);

        Task AddField(CreateFieldRequest request);

        Task CreateFieldValueVariant(CreateFieldValueVariantRequest request);

        Task UpdateFieldValueVariant(UpdateFieldValueVariantRequest request);

        Task<ProjectFieldDropdownValue> DeleteFieldValueVariant(int projectId,
            int projectFieldId,
            int valueId);

        Task MoveField(int projectid, int projectcharacterfieldid, short direction);

        Task MoveFieldVariant(int projectid,
            int projectFieldId,
            int projectFieldVariantId,
            short direction);

        Task CreateFieldValueVariants(int projectId,
            int projectFieldId,
            [NotNull]
            string valuesToAdd);

        Task MoveFieldAfter(int projectId, int projectFieldId, int? afterFieldId);

        Task SetFieldSettingsAsync(FieldSettingsRequest request);
    }

    public class FieldSettingsRequest 
    {
        public int? NameField { get; set; }
        public int? DescriptionField { get; set; }
        public bool LegacyModelEnabled { get; set; }
        public int ProjectId { get; set; }
    }

    public abstract class FieldRequestBase
    {
        protected FieldRequestBase(int projectId,
            string name,
            string fieldHint,
            bool canPlayerEdit,
            bool canPlayerView,
            bool isPublic,
            MandatoryStatus mandatoryStatus,
            IReadOnlyCollection<int> showForGroups,
            bool validForNpc,
            bool includeInPrint,
            bool showForUnapprovedClaims,
            int price,
            string masterFieldHint,
            string programmaticValue)
        {
            ProjectId = projectId;
            Name = name;
            FieldHint = fieldHint;
            CanPlayerEdit = canPlayerEdit;
            CanPlayerView = canPlayerView;
            IsPublic = isPublic;
            MandatoryStatus = mandatoryStatus;
            ShowForGroups = showForGroups;
            ValidForNpc = validForNpc;
            IncludeInPrint = includeInPrint;
            ShowForUnapprovedClaims = showForUnapprovedClaims;
            Price = price;
            MasterFieldHint = masterFieldHint;
            ProgrammaticValue = programmaticValue;
        }

        public int ProjectId { get; }
        public string Name { get; }
        public string FieldHint { get; }

        public bool CanPlayerEdit { get; }
        public bool CanPlayerView { get; }
        public bool IsPublic { get; }
        public MandatoryStatus MandatoryStatus { get; }
        public IReadOnlyCollection<int> ShowForGroups { get; }
        public bool ValidForNpc { get; }
        public bool IncludeInPrint { get; }
        public bool ShowForUnapprovedClaims { get; }
        public int Price { get; }
        public string MasterFieldHint { get; }
        public string ProgrammaticValue { get; }
    }

    public sealed class CreateFieldRequest : FieldRequestBase
    {
        public ProjectFieldType FieldType { get; }
        public FieldBoundTo FieldBoundTo { get; }

        /// <inheritdoc />
        public CreateFieldRequest(int projectId,
            ProjectFieldType fieldType,
            string name,
            string fieldHint,
            bool canPlayerEdit,
            bool canPlayerView,
            bool isPublic,
            FieldBoundTo fieldBoundTo,
            MandatoryStatus mandatoryStatus,
            IReadOnlyCollection<int> showForGroups,
            bool validForNpc,
            bool includeInPrint,
            bool showForUnapprovedClaims,
            int price,
            string masterFieldHint,
            string programmaticValue) : base(projectId,
            name,
            fieldHint,
            canPlayerEdit,
            canPlayerView,
            isPublic,
            mandatoryStatus,
            showForGroups,
            validForNpc,
            includeInPrint,
            showForUnapprovedClaims,
            price,
            masterFieldHint,
            programmaticValue)
        {
            FieldType = fieldType;
            FieldBoundTo = fieldBoundTo;
        }
    }

    public sealed class UpdateFieldRequest : FieldRequestBase
    {
        /// <inheritdoc />
        public UpdateFieldRequest(int projectId,
            string name,
            string fieldHint,
            bool canPlayerEdit,
            bool canPlayerView,
            bool isPublic,
            MandatoryStatus mandatoryStatus,
            IReadOnlyCollection<int> showForGroups,
            bool validForNpc,
            bool includeInPrint,
            bool showForUnapprovedClaims,
            int price,
            string masterFieldHint,
            int projectFieldId,
            string programmaticValue) : base(projectId,
            name,
            fieldHint,
            canPlayerEdit,
            canPlayerView,
            isPublic,
            mandatoryStatus,
            showForGroups,
            validForNpc,
            includeInPrint,
            showForUnapprovedClaims,
            price,
            masterFieldHint,
            programmaticValue)
        {
            ProjectFieldId = projectFieldId;
        }

        public int ProjectFieldId { get; }
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
        public TimeSlotOptions TimeSlotOptions { get; }

        protected FieldValueVariantRequestBase(int projectId,
            string label,
            string description,
            int projectFieldId,
            string masterDescription,
            string programmaticValue,
            int price,
            bool playerSelectable,
            TimeSlotOptions timeSlotOptions)
        {
            ProjectId = projectId;
            Label = label;
            Description = description;
            ProjectFieldId = projectFieldId;
            MasterDescription = masterDescription;
            ProgrammaticValue = programmaticValue;
            Price = price;
            PlayerSelectable = playerSelectable;
            TimeSlotOptions = timeSlotOptions;
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
            bool playerSelectable,
            TimeSlotOptions timeSlotOptions)
            : base(projectId,
                label,
                description,
                projectFieldId,
                masterDescription,
                programmaticValue,
                price,
                playerSelectable,
                timeSlotOptions)
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
            bool playerSelectable,
            Domain.Schedules.TimeSlotOptions timeSlotOptions)
            : base(projectId,
                label,
                description,
                projectFieldId,
                masterDescription,
                programmaticValue,
                price,
                playerSelectable,
                timeSlotOptions)
        {
        }
    }
}
