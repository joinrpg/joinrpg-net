using System.ComponentModel.DataAnnotations;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Accommodation.Test;

/// <summary>
/// Валидация формы типа поселения: сообщения об ошибках видит мастер, поэтому они должны быть
/// русскими и осмысленными (issue #3331 — «must be between 1 and 2147483647»).
/// </summary>
public class RoomTypeEditViewModelTest
{
    [Fact]
    public void EmptyFormIsPrefilledWithDefaultCapacity()
    {
        // Иначе в поле отрисуется 0 (дефолт int) и первый же сабмит упирается в валидацию.
        new RoomTypeEditViewModel().Capacity.ShouldBe(RoomTypeEditViewModel.DefaultCapacity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(RoomTypeEditViewModel.MaxCapacity + 1)]
    public void InvalidCapacityReportsRussianMessage(int capacity)
    {
        var errors = Validate(new RoomTypeEditViewModel { Name = "Шатёр", Capacity = capacity });

        errors.ShouldContain("Укажите количество мест в номере — целое число от 1 до 1000");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(RoomTypeEditViewModel.MaxCapacity)]
    public void ValidCapacityPassesValidation(int capacity)
    {
        Validate(new RoomTypeEditViewModel { Name = "Шатёр", Capacity = capacity }).ShouldBeEmpty();
    }

    [Fact]
    public void NegativeCostReportsRussianMessage()
    {
        var errors = Validate(new RoomTypeEditViewModel { Name = "Шатёр", Capacity = 2, Cost = -1 });

        errors.ShouldContain("Цена за 1 место не может быть отрицательной");
    }

    [Fact]
    public void MissingNameReportsRussianMessage()
    {
        var errors = Validate(new RoomTypeEditViewModel { Name = "", Capacity = 2 });

        errors.ShouldContain("Укажите название типа поселения");
    }

    private static IReadOnlyList<string?> Validate(RoomTypeEditViewModel model)
    {
        var results = new List<ValidationResult>();
        _ = Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return [.. results.Select(r => r.ErrorMessage)];
    }
}
