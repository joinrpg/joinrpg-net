using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using JetBrains.Annotations;
using Joinrpg.Markdown;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models
{

    public enum ProjectFieldViewType
    {
        [Display(Name = "Строка", Order = 1), UsedImplicitly]
        String,
        [Display(Name = "Текст", Order = 2), UsedImplicitly]
        Text,
        [Display(Name = "Выбор", Order = 3), UsedImplicitly]
        Dropdown,
        [Display(Name = "Чекбокс", Order = 5), UsedImplicitly]
        Checkbox,
        [Display(Name = "Мультивыбор", Order = 4), UsedImplicitly]
        MultiSelect,
        [Display(Name = "Заголовок", Order = 6), UsedImplicitly]
        Header,
        [Display(Name = "Число", Order = 7), UsedImplicitly]
        Number,
        [Display(
            Name = "Логин/идентификатор",
            Description = "Латинские буквы и цифры. Если вам нужен логин во внешнюю систему, адрес игровой электронной почты, блога etc",
            Order = 8)]
        [UsedImplicitly]
        Login,
    }

    public static class ProjectFieldViewTypeHelper
    {
        /// <summary>
        /// Returns true if field supports price calculations
        /// </summary>
        public static bool SupportsPricing(this ProjectFieldViewType self)
            => ((ProjectFieldType)self).SupportsPricing();

        /// <summary>
        /// Returns true if price could be entered for field, not for it's values
        /// </summary>
        public static bool SupportsPricingOnField(this ProjectFieldViewType self)
            => ((ProjectFieldType)self).SupportsPricingOnField();

        /// <summary>
        /// Returns true if field has predefined values
        /// </summary>
        public static bool HasValuesList(this ProjectFieldViewType self)
            => ((ProjectFieldType)self).HasValuesList();
    }

    public enum FieldBoundToViewModel
    {
        [Display(
             Name = "персонажу",
             Description = "Все, что связано с персонажем, его умения, особенности, предыстория. Выбирайте этот вариант по умолчанию.")]
        [UsedImplicitly]
        Character,

        [Display(
             Name = "заявке",
             Description = "всё, что связано с конкретным игроком: пожелания по завязкам, направлению игры и т.п. После отклонения принятой заявки они не будут доступны новому игроку на этой роли.")]
        [UsedImplicitly]
        Claim,
    }

    public enum MandatoryStatusViewType
    {
        [Display(Name = "Опциональное"), UsedImplicitly]
        Optional,

        [Display(Name = "Рекомендованное",
            Description = "При незаполненном поле будет выдаваться предупреждение, а заявка или персонаж — помечаться как проблемные"),
            UsedImplicitly]
        Recommended,

        [Display(Name = "Обязательное",
            Description = "Сохранение с незаполенным полем будет невозможно."),
            UsedImplicitly]
        Required,
    }

    public class GameFieldViewModelBase: IValidatableObject, IProjectIdAware
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
        public IHtmlString DescriptionDisplay { get; set; }

        [Display(Name = "Описание (только для мастеров)")]
        public IHtmlString MasterDescriptionDisplay { get; set; }

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
                yield return validationResult;
        }

        protected virtual IEnumerable<ValidationResult> ValidateCore()
        {
            return Enumerable.Empty<ValidationResult>();
        }
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

        [ReadOnly(true)]
        public bool WasEverUsed { get; set; }

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

            FillNotEditable(field, currentUserId);
        }

        public void FillNotEditable(ProjectField field, int currentUserId)
        {
            DropdownValues = field.GetOrderedValues()
                .Select(fv => new GameFieldDropdownValueListItemViewModel(fv))
                .MarkFirstAndLast();
            FieldViewType = (ProjectFieldViewType) field.FieldType;
            FieldBoundTo = (FieldBoundToViewModel) field.FieldBoundTo;
            IsActive = field.IsActive;
            HasValueList = field.HasValueList();
            WasEverUsed = field.WasEverUsed;
            CanEditFields = field.HasMasterAccess(currentUserId, acl => acl.CanChangeFields);
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
    }

    public class GameFieldCreateViewModel: GameFieldViewModelBase
    {
        [Display(Name = "Тип поля")]
        public ProjectFieldViewType FieldViewType { get; set; }

        [Display(Name = "Описание"), UIHint("MarkdownString")]
        public string DescriptionEditable { get; set; }

        [Display(Name = "Описание (для мастеров)"), UIHint("MarkdownString")]
        public string MasterDescriptionEditable { get; set; }

        [Display(Name = "Привязано к")]
        public FieldBoundToViewModel FieldBoundTo { get; set; }

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
        public int ProjectId { get; }
        public IEnumerable<GameFieldEditViewModel> Items { get; }

        public bool CanEditFields { get; }

        public GameFieldListViewModel(Project project, int currentUserId)
        {
            ProjectId = project.ProjectId;
            Items = project.GetOrderedFields().ToViewModels(currentUserId);
            CanEditFields = project.HasMasterAccess(currentUserId, pa => pa.CanChangeFields) && project.Active;
        }
    }

    /// <summary>
    /// Base view class for dropdown value
    /// </summary>
    public abstract class GameFieldDropdownValueViewModelBase
    {
        [Display(Name = "Значение"), Required]
        public string Label { get; set; }

        [Display(Name = "Описание"), UIHint("MarkdownString")]
        public string Description { get; set; }

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


        public GameFieldDropdownValueViewModelBase(ProjectField field)
        {
            FieldName = field.FieldName;
            ProjectId = field.ProjectId;
            ProjectFieldId = field.ProjectFieldId;
            PlayerSelectable = CanPlayerEditField = field.CanPlayerEdit;
        }

        public GameFieldDropdownValueViewModelBase() { }
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
    public class GameFieldDropdownValueEditViewModel: GameFieldDropdownValueViewModelBase
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
        }

        public GameFieldDropdownValueEditViewModel() { }//For binding
    }

    /// <summary>
    /// View class for creating dropdown value
    /// </summary>
    public class GameFieldDropdownValueCreateViewModel: GameFieldDropdownValueViewModelBase
    {
        public GameFieldDropdownValueCreateViewModel(ProjectField field) : base(field)
        {
            Label = $"Вариант {field.DropdownValues.Count + 1}";
        }

        public GameFieldDropdownValueCreateViewModel() { }//For binding
    }

    public static class GameFieldViewModelsExtensions
    {
        public static IEnumerable<GameFieldEditViewModel> ToViewModels(this IEnumerable<ProjectField> gameFields, int currentUserId)
        {
            return gameFields.Select(pf => new GameFieldEditViewModel(pf, currentUserId)).MarkFirstAndLast();
        }
    }
}
