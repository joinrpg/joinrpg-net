using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Schedules;
using JoinRpg.Helpers.Web;
using JoinRpg.Markdown;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models.FieldSetup
{

    public interface IFieldNavigationAware : IProjectIdAware
    {
        FieldNavigationModel Navigation { get; }
        new int ProjectId { get; set; }

        void SetNavigation(FieldNavigationModel fieldNavigation);
    }

    public abstract class GameFieldViewModelBase : IValidatableObject, IFieldNavigationAware
    {
        public int ProjectId { get; set; }

        [Display(Name = "Название поля"), Required]
        public string Name { get; set; }

        [Display(Name = "Публичное (видно всем)")]
        public bool IsPublic { get; set; }

        [Display(Name = "Видно игроку")]
        public bool CanPlayerView { get; set; }

        [Display(
            Name = "Игрок может менять",
            Description = "Для полей типа выбор/мультивыбор можно запретить выставление каждого значения в отдельности в свойствах значения")]
        public bool CanPlayerEdit { get; set; }

        [Display(Name = "Описание")]
        public JoinHtmlString DescriptionDisplay { get; set; }

        [Display(Name = "Описание (только для мастеров)")]
        public JoinHtmlString MasterDescriptionDisplay { get; set; }

        [Display(Name = "Обязательное?")]
        public MandatoryStatusViewType MandatoryStatus { get; set; }

        [Display(Name = "Показывать только для групп", Description = "Если оставить пустым, будет показываться всегда")]
        public ICollection<string> ShowForGroups { get; set; } = new List<string>();

        [Display(Name = "Доступно NPC", Description = "Доступно для персонажей-NPC")]
        public bool ValidForNpc { get; set; }

        [Display(Name = "Показывать даже при непринятой заявке")]
        public bool ShowForUnApprovedClaim { get; set; } = true;

        [Display(Name = "Цена", Description = "Цена будет добавлена ко взносу")]
        public int Price { get; set; } = 0;

        public FieldNavigationModel Navigation { get; set; }

        public abstract void SetNavigation(FieldNavigationModel navigationModel);

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (IsPublic && !CanPlayerView)
            {
                yield return new ValidationResult("Нельзя скрыть публичное поле от игрока.",
                  new[] { nameof(CanPlayerView), nameof(IsPublic) });
            }
            if (CanPlayerEdit && !CanPlayerView)
            {
                yield return
                  new ValidationResult("Нельзя скрыть поле от игрока и одновременно разрешить редактирование поля.",
                    new[] { nameof(CanPlayerView), nameof(CanPlayerEdit) });
            }

            foreach (var validationResult in ValidateCore())
            {
                yield return validationResult;
            }
        }

        protected virtual IEnumerable<ValidationResult> ValidateCore() => Enumerable.Empty<ValidationResult>();
    }

    public class GameFieldEditViewModel : GameFieldViewModelBase, IMovableListItem
    {
        public int ProjectFieldId { get; set; }

        [ReadOnly(true)]
        public bool HasValueList { get; private set; }

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

    public class GameFieldCreateViewModel : GameFieldViewModelBase
    {
        [Display(Name = "Тип поля")]
        public ProjectFieldViewType FieldViewType { get; set; }

        [Display(Name = "Описание"), UIHint("MarkdownString")]
        public string DescriptionEditable { get; set; }

        [Display(Name = "Описание (для мастеров)"), UIHint("MarkdownString")]
        public string MasterDescriptionEditable { get; set; }

        [Display(Name = "Привязано к")]
        public FieldBoundToViewModel FieldBoundTo { get; set; }

        public override void SetNavigation(FieldNavigationModel navigationModel)
        {
            navigationModel.Page = FieldNavigationPage.AddField;
            Navigation = navigationModel;
        }

        protected override IEnumerable<ValidationResult> ValidateCore()
        {
            if (FieldBoundTo == FieldBoundToViewModel.Claim && ValidForNpc)
            {
                yield return
                  new ValidationResult("Невозможно разрешить NPC поля, связанные с заявкой.",
                    new List<string> { nameof(DataModel.FieldBoundTo), nameof(ValidForNpc) });
            }
            if (Price != 0 && !FieldViewType.SupportsPricingOnField())
            {
                yield return new ValidationResult(
                    $"Поле {FieldViewType} не поддерживает ввод цены.");
            }
        }
    }

    public class GameFieldListViewModel
    {
        public IEnumerable<GameFieldEditViewModel> Items { get; }

        public FieldNavigationModel Navigation { get; }

        public GameFieldListViewModel(FieldNavigationModel navigation, IEnumerable<GameFieldEditViewModel> fields)
        {
            Navigation = navigation;
            Items = fields;
        }
    }

    /// <summary>
    /// Base view class for dropdown value
    /// </summary>
    public abstract class GameFieldDropdownValueViewModelBase
    {
        [Display(Name = "Значение"), Required]
        public string Label { get; set; }

        // ReSharper disable once Mvc.TemplateNotResolved
        [Display(Name = "Описание"), UIHint("MarkdownString")]
        public string Description { get; set; }

        // ReSharper disable once Mvc.TemplateNotResolved
        [Display(Name = "Описание для мастеров"), UIHint("MarkdownString")]
        public string MasterDescription { get; set; }

        [Display(Name = "Цена", Description = "Если это поле заполнено, то цена будет добавлена ко взносу")]
        public int Price { get; set; } = 0;

        [Display(Name = "Игрок может выбрать", Description = "Если снять эту галочку, то игрок не сможет выбрать этот вариант, только мастер")]
        public bool PlayerSelectable { get; set; } = true;

        [Display(Name = "Программный ID",
            Description = "Используется для передачи во внешние ИТ-системы игры, если они есть. Значение определяется программистами внешней системы. Игнорируйте это поле, если у вас на игре нет никакой ИТ-системы")]
        public string ProgrammaticValue { get; set; }

        public int ProjectId { get; set; }
        public int ProjectFieldId { get; set; }
        public string FieldName { get; }
        public bool CanPlayerEditField { get; }


        [Display(Name = "Длина тайм-слота (в минутах")]
        public int TimeSlotInMinutes { get; set; }

        [Display(Name = "Начало тайм-слота", Description = "В формате ГГГГ-ММ-ДДTЧЧ:ММ+03:00. Если таймзона не указывается, подразумевается московское.")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mmK}", ApplyFormatInEditMode = true)]
        public DateTimeOffset TimeSlotStartTime { get; set; }

        [ReadOnly(true)]
        public bool IsTimeField { get; set; }


        public GameFieldDropdownValueViewModelBase(ProjectField field)
        {
            FieldName = field.FieldName;
            ProjectId = field.ProjectId;
            ProjectFieldId = field.ProjectFieldId;
            PlayerSelectable = CanPlayerEditField = field.CanPlayerEdit;
            IsTimeField = field.IsTimeSlot();
        }

        public GameFieldDropdownValueViewModelBase() { }

        public TimeSlotOptions GetTimeSlotRequest(ProjectField field, string value)
        {
            return field.IsTimeSlot()
                ? new TimeSlotOptions
                {
                    StartTime = DateTimeOffset.ParseExact(
                        value,
                        "yyyy-MM-ddTHH:mmK",
                        System.Globalization.CultureInfo.InvariantCulture),
                    TimeSlotInMinutes = TimeSlotInMinutes
                }
                : null;
        }
    }

    /// <summary>
    /// View class for displaying dropdown value in field's editor
    /// </summary>
    public class GameFieldDropdownValueListItemViewModel : IMovableListItem
    {
        [Display(Name = "Значение"), Required]
        public string Label { get; set; }

        [Display(Name = "Описание")]
        public string Description { get; }

        [Display(Name = "Цена")]
        public int Price { get; }

        public int ProjectId { get; }
        public int ProjectFieldId { get; }
        public bool IsActive { get; }

        public bool MasterRestricted { get; }

        public int? CharacterGroupId { get; }

        public int ValueId { get; }

        public GameFieldDropdownValueListItemViewModel(ProjectFieldDropdownValue value)
        {
            Label = value.Label;
            Description = value.Description.ToPlainText().ToString();
            IsActive = value.IsActive;
            Price = value.Price;
            ProjectId = value.ProjectId;
            ProjectFieldId = value.ProjectFieldId;
            ValueId = value.ProjectFieldDropdownValueId;
            CharacterGroupId = value.CharacterGroup?.CharacterGroupId;
            MasterRestricted = !value.PlayerSelectable && value.ProjectField.CanPlayerEdit;
        }

        #region Implementation of IMovableListItem

        public bool First { get; set; }
        public bool Last { get; set; }
        int IMovableListItem.ItemId => ValueId;

        #endregion
    }

    /// <summary>
    /// View class for editing dropdown value
    /// </summary>
    public class GameFieldDropdownValueEditViewModel : GameFieldDropdownValueViewModelBase
    {
        public bool IsActive { get; set; }

        public int ProjectFieldDropdownValueId { get; set; }

        public GameFieldDropdownValueEditViewModel(ProjectField field, ProjectFieldDropdownValue value) : base(field)
        {
            Label = value.Label;
            Description = value.Description.Contents;
            IsActive = value.IsActive;
            Price = value.Price;
            ProjectFieldDropdownValueId = value.ProjectFieldDropdownValueId;
            PlayerSelectable = value.PlayerSelectable;
            ProgrammaticValue = value.ProgrammaticValue;
            if (field.IsTimeSlot())
            {
                var options = value.GetTimeSlotOptions();
                TimeSlotInMinutes = options.TimeSlotInMinutes;
                TimeSlotStartTime = options.StartTime;
            }
        }

        public GameFieldDropdownValueEditViewModel() { }//For binding
    }

    /// <summary>
    /// View class for creating dropdown value
    /// </summary>
    public class GameFieldDropdownValueCreateViewModel : GameFieldDropdownValueViewModelBase
    {
        public GameFieldDropdownValueCreateViewModel(ProjectField field) : base(field)
        {
            Label = $"Вариант {field.DropdownValues.Count + 1}";
            if (field.IsTimeSlot())
            {
                var options = field.GetDefaultTimeSlotOptions();
                TimeSlotInMinutes = options.TimeSlotInMinutes;
                TimeSlotStartTime = options.StartTime;
            }
        }

        public GameFieldDropdownValueCreateViewModel() { }//For binding
    }

    public static class GameFieldViewModelsExtensions
    {
        public static IEnumerable<GameFieldEditViewModel> ToViewModels(this IEnumerable<ProjectField> gameFields, int currentUserId) => gameFields.Select(pf => new GameFieldEditViewModel(pf, currentUserId)).MarkFirstAndLast();
    }
}
