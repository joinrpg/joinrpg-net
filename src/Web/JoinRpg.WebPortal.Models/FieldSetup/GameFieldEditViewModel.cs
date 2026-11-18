using JoinRpg.Markdown;
using JoinRpg.Web.ProjectMasterTools.Fields;

namespace JoinRpg.WebPortal.Models.FieldSetup;

public class GameFieldEditViewModel : GameFieldViewModelBase
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

    public GameFieldEditViewModel(ProjectFieldInfo field)
    {
        CanPlayerView = field.CanPlayerView;
        CanPlayerEdit = field.CanPlayerEdit;
        DescriptionEditable = field.Description?.Value ?? "";
        MasterDescriptionEditable = field.MasterDescription?.Value ?? "";
        DescriptionDisplay = field.Description.ToHtmlString();
        MasterDescriptionDisplay = field.MasterDescription.ToHtmlString();
        ProjectFieldId = field.Id.ProjectFieldId;
        IsPublic = field.IsPublic;
        IsNameField = field.IsName;
        Name = field.Name;
        ProjectId = field.Id.ProjectId;
        MandatoryStatus = (MandatoryStatusViewType)field.MandatoryStatus;
        ShowForGroups = [.. field.GroupsAvailableForIds];
        IncludeInPrint = field.IncludeInPrint;
        ValidForNpc = field.ValidForNpc;
        ShowForUnApprovedClaim = field.ShowOnUnApprovedClaims;
        Price = field.Price;
        ProgrammaticValue = field.ProgrammaticValue ?? "";
        FillNotEditable(field);
    }

    public void FillNotEditable(ProjectFieldInfo field)
    {
        DropdownValues = field.SortedVariants
            .Select(v => new FieldVariantListItemViewModel
            {
                VariantId = v.Id,
                Label = v.Label,
                Description = v.Description.ToPlainTextWithoutHtmlEscape(),
                Price = v.Price,
                IsActive = v.IsActive,
                MasterRestricted = !v.IsPlayerSelectable && field.CanPlayerEdit,
                WasEverUsed = v.WasEverUsed,
            })
            .ToList();
        FieldViewType = (ProjectFieldViewType)field.Type;
        FieldBoundTo = (FieldBoundToViewModel)field.BoundTo;
        IsActive = field.IsActive;
        HasValueList = field.HasValueList;
        SupportsMassAdding = field.SupportsMassAdding;
        IsTimeSlot = field.IsTimeSlot;
        TimeSlotMassAdd = field.IsTimeSlot ? CreateTimeSlotMassAdd(field) : null;
    }

    [ReadOnly(true)]
    public bool IsTimeSlot { get; private set; }

    private static TimeSlotMassAddViewModel CreateTimeSlotMassAdd(ProjectFieldInfo field)
    {
        var lastOptions = (field.LastVariant as TimeSlotFieldVariant)?.TimeSlotOptions ?? TimeSlotOptions.CreateDefault(field.ProjectTimeZone);
        return new TimeSlotMassAddViewModel(
            field.Id,
            DateOnly.FromDateTime(lastOptions.LocalEndTime),
            lastOptions.TimeSlotInMinutes);
    }

    /// <summary>
    /// Параметры массового добавления таймслотов; null, если поле не таймслот
    /// </summary>
    [ReadOnly(true)]
    public TimeSlotMassAddViewModel? TimeSlotMassAdd { get; private set; }

    public GameFieldEditViewModel()
    { }

    [ReadOnly(true)]
    public IList<FieldVariantListItemViewModel> DropdownValues { get; private set; } = [];

    [Display(Name = "Тип поля"), ReadOnly(true)]
    public ProjectFieldViewType FieldViewType { get; private set; }

    [Display(Name = "Привязано к"), ReadOnly(true)]
    public FieldBoundToViewModel FieldBoundTo { get; private set; }

    [ReadOnly(true)]
    public bool IsActive { get; private set; }

    /// <summary>
    /// Поле привязано к имени персонажа. Постится скрытым полем: нужно правилу валидации
    /// на POST, окончательную проверку всё равно делает сервис (#965).
    /// </summary>
    [ReadOnly(true)]
    public bool IsNameField { get; set; }

    [Display(Name = "Включать в распечатки")]
    public bool IncludeInPrint { get; set; } = true;

    protected override IEnumerable<ValidationResult> ValidateCore()
    {
        // Сервер разрешает править старое непубличное поле-имя, если публичность не меняется,
        // но из интерфейса намеренно требуем сразу сделать его публичным — так данные лечатся,
        // а мастер видит требование, а не молчаливое расхождение настроек (#965).
        if (IsNameField && !IsPublic)
        {
            yield return
                new ValidationResult("Поле, привязанное к имени персонажа, должно быть публичным (видно всем): имена персонажей показываются всем посетителям сайта.");
        }
        if (!CanPlayerView && IncludeInPrint)
        {
            yield return
                new ValidationResult("Невозможно включить в распечатки поле, скрытое от игрока.");
        }
        if (!CanPlayerView && FieldViewType.SupportsPricing()
                && ((DropdownValues.Any(v => v.Price != 0)) || Price != 0))
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
