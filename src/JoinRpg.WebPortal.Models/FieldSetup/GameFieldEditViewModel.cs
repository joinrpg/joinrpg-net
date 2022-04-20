using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Markdown;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models.FieldSetup
{
    public class GameFieldEditViewModel : GameFieldViewModelBase, IMovableListItem
    {
        public int ProjectFieldId { get; set; }

        [ReadOnly(true)]
        public bool HasValueList { get; private set; }

        [ReadOnly(true)]
        public bool SupportsMassAdding { get; private set; }

        [Display(Name = "Описание"), UIHint("MarkdownString")]
        public string DescriptionEditable { get; set; }

        [Display(Name = "Описание (только для мастеров)"), UIHint("MarkdownString")]
        public string MasterDescriptionEditable { get; set; }

        [Display(Name = "Программный ID",
            Description = "Используется для передачи во внешние ИТ-системы игры, если они есть. Значение определяется программистами внешней системы. Игнорируйте это поле, если у вас на игре нет никакой ИТ-системы")]
        public string ProgrammaticValue { get; set; }

        [ReadOnly(true)]
        public bool WasEverUsed { get; set; }

        [ReadOnly(true)]
        public bool IsTimeField { get; set; }

        public GameFieldEditViewModel(ProjectField field, int currentUserId)
        {
            CanPlayerView = field.CanPlayerView;
            CanPlayerEdit = field.CanPlayerEdit;
            DescriptionEditable = field.Description.Contents;
            MasterDescriptionEditable = field.MasterDescription.Contents;
            DescriptionDisplay = field.Description.ToHtmlString();
            MasterDescriptionDisplay = field.MasterDescription.ToHtmlString();
            ProjectFieldId = field.ProjectFieldId;
            IsPublic = field.IsPublic;
            Name = field.FieldName;
            ProjectId = field.ProjectId;
            MandatoryStatus = (MandatoryStatusViewType)field.MandatoryStatus;
            ShowForGroups = field
                .GroupsAvailableFor
                .Select(c => c.CharacterGroupId)
                .PrefixAsGroups()
                .ToList();
            IncludeInPrint = field.IncludeInPrint;
            ValidForNpc = field.ValidForNpc;
            ShowForUnApprovedClaim = field.ShowOnUnApprovedClaims;
            Price = field.Price;
            ProgrammaticValue = field.ProgrammaticValue;

            FillNotEditable(field, currentUserId);
        }

        public void FillNotEditable(ProjectField field, int currentUserId)
        {
            DropdownValues = field.GetOrderedValues()
                .Select(fv => new GameFieldDropdownValueListItemViewModel(fv))
                .MarkFirstAndLast();
            FieldViewType = (ProjectFieldViewType)field.FieldType;
            FieldBoundTo = (FieldBoundToViewModel)field.FieldBoundTo;
            IsActive = field.IsActive;
            HasValueList = field.HasValueList();
            WasEverUsed = field.WasEverUsed;
            CanEditFields = field.HasMasterAccess(currentUserId, acl => acl.CanChangeFields);
            CanDeleteField = CanEditFields && !field.IsName() && !field.IsRoomSlot() && !field.IsTimeSlot();
            IsTimeField = field.IsTimeSlot();
            SupportsMassAdding = field.SupportsMassAdding();

        }

        public GameFieldEditViewModel()
        { }

        [ReadOnly(true)]
        public IEnumerable<GameFieldDropdownValueListItemViewModel> DropdownValues { get; private set; }

        [Display(Name = "Тип поля"), ReadOnly(true)]
        public ProjectFieldViewType FieldViewType { get; private set; }

        [Display(Name = "Привязано к"), ReadOnly(true)]
        public FieldBoundToViewModel FieldBoundTo { get; private set; }

        [ReadOnly(true)]
        public bool IsActive { get; private set; }

        public bool First { get; set; }

        public bool Last { get; set; }

        int IMovableListItem.ItemId => ProjectFieldId;

        public bool CanEditFields { get; private set; }
        public bool CanDeleteField { get; private set; }
        [Display(Name = "Включать в распечатки")]
        public bool IncludeInPrint { get; set; } = true;

        protected override IEnumerable<ValidationResult> ValidateCore()
        {
            if (!CanPlayerView && IncludeInPrint)
            {
                yield return
                    new ValidationResult("Невозможно включить в распечатки поле, скрытое от игрока.");
            }
            if (!CanPlayerView && FieldViewType.SupportsPricing()
                    && ((FieldViewType.HasValuesList() && DropdownValues.Any(v => v.Price != 0)) || Price != 0))
            {
                yield return
                    new ValidationResult("Нельзя скрыть от игрока поле, влияющее на размер взноса.");
            }
        }

        public override void SetNavigation(FieldNavigationModel navigationModel)
        {
            navigationModel.Page = FieldNavigationPage.EditField;
            Navigation = navigationModel;
        }
    }
}
