using JoinRpg.DataModel.Mocks;
using JoinRpg.Web.Models.Characters;

namespace JoinRpg.WebPortal.Models.Test;

public class CharacterViewModelTest
{
    private readonly MockedProject _mock = new();

    [Fact]
    public void AddCharacterViewModelHaveSimpleFields()
    {
        var field = _mock.CreateField("Char1");
        var vm = new AddCharacterViewModel().Fill(_mock.Group, _mock.Master.UserId, _mock.ProjectInfo);
        var fieldView = vm.Fields.Field(field);

        _ = fieldView.ShouldNotBeNull();
        fieldView.Value.ShouldBeNull();
        fieldView.ShouldBeHidden();
        fieldView.ShouldBeEditable();

    }

    [Fact]
    public void AddCharacterViewModelWithConditionalField()
    {
        var field = _mock.CreateConditionalField(_mock.Group);
        var vm = new AddCharacterViewModel().Fill(_mock.Group, _mock.Master.UserId, _mock.ProjectInfo);
        var fieldView = vm.Fields.Field(field);

        _ = fieldView.ShouldNotBeNull();
        fieldView.Value.ShouldBeNull();
        fieldView.ShouldBeHidden();
        fieldView.ShouldBeEditable();

    }

    [Fact]
    public void AddCharacterViewModelWithoutConditionalField()
    {
        var field = _mock.CreateConditionalField(_mock.Group);

        var groupForClaim = _mock.CreateCharacterGroup();
        var vm = new AddCharacterViewModel().Fill(groupForClaim, _mock.Master.UserId, _mock.ProjectInfo);
        var fieldView = vm.Fields.Field(field);

        _ = fieldView.ShouldNotBeNull();
        fieldView.Value.ShouldBeNull();
        fieldView.ShouldBeHidden();
        fieldView.ShouldBeReadonly();

    }

}
