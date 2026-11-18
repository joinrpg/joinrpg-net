using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Portal.Controllers;
using JoinRpg.WebPortal.Models.FieldSetup;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Test.Controllers;

/// <summary>
/// Начало таймслота в форме значения: вне допустимых границ значение не сохраняется — иначе
/// вариант с таким началом уронил бы загрузку метаданных проекта.
/// </summary>
public class GameFieldControllerTimeSlotValueTest
{
    private const string ErrorMessage = "Укажите начало тайм-слота с 2000 по 2099 год";
    private const string FieldName = "Время";

    [Fact]
    public async Task StartTimeOutOfRange_ShowsErrorAndKeepsTimeSlotFields()
    {
        var (controller, viewModel) = Arrange();
        viewModel.TimeSlotStartTime = new DateTime(1, 1, 1);

        var result = await controller.CreateValue(viewModel);

        result.ShouldBeOfType<ViewResult>();
        // Перерисованная форма: поля таймслота, заголовок и галочка для игрока на месте
        viewModel.IsTimeField.ShouldBeTrue();
        viewModel.FieldName.ShouldBe(FieldName);
        viewModel.CanPlayerEditField.ShouldBeTrue();
        controller.ModelState[nameof(viewModel.TimeSlotStartTime)].ShouldNotBeNull()
            .Errors.ShouldHaveSingleItem().ErrorMessage.ShouldBe(ErrorMessage);
    }

    [Fact]
    public async Task NonPositiveLength_ShowsError()
    {
        var (controller, viewModel) = Arrange();
        viewModel.TimeSlotInMinutes = -2_000_000_000;

        var result = await controller.CreateValue(viewModel);

        result.ShouldBeOfType<ViewResult>();
        controller.ModelState[nameof(viewModel.TimeSlotInMinutes)].ShouldNotBeNull()
            .Errors.ShouldHaveSingleItem().ErrorMessage.ShouldBe("Длина тайм-слота должна быть больше нуля");
        controller.ModelState[nameof(viewModel.TimeSlotStartTime)].ShouldBeNull();
    }

    /// <summary>
    /// Ошибка привязки (нераспознанное значение) заменяется русским сообщением, а введённое значение
    /// остаётся для перерисовки формы. Начало в модели валидное — срабатывает именно ветка ошибки привязки.
    /// </summary>
    [Fact]
    public async Task BindingError_IsReplacedWithRussianMessage()
    {
        var (controller, viewModel) = Arrange();
        controller.ModelState.SetModelValue(nameof(viewModel.TimeSlotStartTime), "мусор", "мусор");
        controller.ModelState.AddModelError(nameof(viewModel.TimeSlotStartTime), "The value 'мусор' is invalid.");

        var result = await controller.CreateValue(viewModel);

        result.ShouldBeOfType<ViewResult>();
        var entry = controller.ModelState[nameof(viewModel.TimeSlotStartTime)].ShouldNotBeNull();
        entry.Errors.ShouldHaveSingleItem().ErrorMessage.ShouldBe(ErrorMessage);
        entry.AttemptedValue.ShouldBe("мусор");
    }

    private static (GameFieldController, GameFieldDropdownValueCreateViewModel) Arrange()
    {
        var mock = new MockedProject();
        var field = mock.AddField(f =>
        {
            f.FieldType = ProjectFieldType.ScheduleTimeSlotField;
            f.FieldName = FieldName;
            f.CanPlayerEdit = true;
        });
        // До проверки начала таймслота контроллер ходит только в метаданные проекта
        var controller = new GameFieldController(
            fieldSetupService: null!,
            manager: null!,
            currentProjectAccessor: null!,
            new FakeProjectMetadataRepository(mock));
        var viewModel = new GameFieldDropdownValueCreateViewModel
        {
            ProjectId = field.Id.ProjectId,
            ProjectFieldId = field.Id.ProjectFieldId,
            Label = "Слот",
            TimeSlotStartTime = new DateTime(2026, 7, 10, 10, 0, 0),
            TimeSlotInMinutes = 50,
        };
        return (controller, viewModel);
    }
}
