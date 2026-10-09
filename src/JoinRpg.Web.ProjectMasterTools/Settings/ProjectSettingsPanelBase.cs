using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace JoinRpg.Web.ProjectMasterTools.Settings;

/// <summary>
/// Общий код панелей настроек проекта: загрузка модели, валидация и сохранение.
/// </summary>
public abstract class ProjectSettingsPanelBase<TModel> : ComponentBase, IDisposable
    where TModel : class
{
    [Inject]
    protected IProjectSettingsClient SettingsClient { get; set; } = null!;

    [Parameter]
    [SupplyParameterFromForm]
    public TModel Model { get; set; } = null!;

    [Parameter]
    public ProjectIdentification ProjectId { get; set; } = null!;

    protected EditContext? editContext;
    protected bool success;
    protected bool formInvalid = true;
    private ValidationMessageStore? messageStore;

    protected abstract Task<TModel> LoadModel(ProjectIdentification projectId);

    protected abstract Task SaveModel(TModel model);

    protected override async Task OnInitializedAsync()
    {
        Model ??= await LoadModel(ProjectId);
        editContext = new(Model);
        editContext.OnFieldChanged += HandleFieldChanged;
        messageStore = new(editContext);
        formInvalid = !editContext.Validate();
        StateHasChanged();
    }

    private void HandleFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        if (editContext is not null)
        {
            success = false;
            // Серверная ошибка относится к прошлой попытке сохранения, иначе она навсегда блокирует кнопку
            messageStore?.Clear();
            formInvalid = !editContext.Validate();
            StateHasChanged();
        }
    }

    /// <summary>
    /// Обработчик OnSubmit, а не OnValidSubmit: EditForm валидирует до вызова обработчика,
    /// и оставшаяся серверная ошибка помешала бы повторной отправке.
    /// </summary>
    protected async Task HandleSubmit()
    {
        if (editContext is null)
        {
            return;
        }
        messageStore?.Clear();
        formInvalid = !editContext.Validate();
        if (formInvalid)
        {
            return;
        }
        try
        {
            await SaveModel(Model);
            success = true;
        }
        catch
        {
            success = false;
            messageStore?.Add(() => Model, "Неизвестная серверная ошибка при сохранении");
        }
        editContext.NotifyValidationStateChanged();
    }

    public void Dispose()
    {
        if (editContext is not null)
        {
            editContext.OnFieldChanged -= HandleFieldChanged;
        }
    }
}
