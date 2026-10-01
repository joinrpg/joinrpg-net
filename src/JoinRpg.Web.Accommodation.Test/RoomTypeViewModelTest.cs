using System.ComponentModel.DataAnnotations;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Web.Models.Accommodation;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Accommodation.Test;

/// <summary>
/// Валидация формы типа поселения: сообщения об ошибках видит мастер, поэтому они должны быть
/// русскими и осмысленными (issue #3331 — «must be between 1 and 2147483647»).
/// </summary>
public class RoomTypeViewModelTest
{
    private readonly MockedProject mock = new();

    [Fact]
    public void EmptyFormIsPrefilledWithDefaultCapacity()
    {
        // Иначе в поле отрисуется 0 (дефолт int) и первый же сабмит упирается в валидацию.
        var vm = new RoomTypeViewModel(new UserIdentification(mock.Master.UserId), mock.ProjectInfo);

        vm.Capacity.ShouldBe(RoomTypeViewModelBase.DefaultCapacity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(RoomTypeViewModelBase.MaxCapacity + 1)]
    public void InvalidCapacityReportsRussianMessage(int capacity)
    {
        var errors = Validate(new RoomTypeViewModel { Name = "Шатёр", Capacity = capacity });

        errors.ShouldContain("Укажите количество мест в номере — целое число от 1 до 1000");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(RoomTypeViewModelBase.MaxCapacity)]
    public void ValidCapacityPassesValidation(int capacity)
    {
        Validate(new RoomTypeViewModel { Name = "Шатёр", Capacity = capacity }).ShouldBeEmpty();
    }

    [Fact]
    public void NegativeCostReportsRussianMessage()
    {
        var errors = Validate(new RoomTypeViewModel { Name = "Шатёр", Capacity = 2, Cost = -1 });

        errors.ShouldContain("Цена за 1 место не может быть отрицательной");
    }

    [Fact]
    public void MissingNameReportsRussianMessage()
    {
        var errors = Validate(new RoomTypeViewModel { Capacity = 2 });

        errors.ShouldContain("Укажите название типа поселения");
    }

    private static IReadOnlyList<string?> Validate(RoomTypeViewModel model)
    {
        var results = new List<ValidationResult>();
        _ = Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return [.. results.Select(r => r.ErrorMessage)];
    }
}
