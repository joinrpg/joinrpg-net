using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Web.Models.Characters;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Test
{
    public class CharacterViewModelTest
    {
        private readonly MockedProject _mock = new MockedProject();

        [Fact]
        public void AddCharacterViewModelHaveSimpleFields()
        {
            var field = _mock.CreateField(new ProjectField());
            var vm = new AddCharacterViewModel().Fill(_mock.Group, _mock.Master.UserId);
            var fieldView = vm.Fields.Field(field);

            fieldView.ShouldNotBeNull();
            fieldView.Value.ShouldBeNull();
            fieldView.ShouldBeHidden();
            fieldView.ShouldBeEditable();

        }

        [Fact]
        public void AddCharacterViewModelWithConditionalField()
        {
            var field = _mock.CreateConditionalField(new ProjectField(), _mock.Group);
            var vm = new AddCharacterViewModel().Fill(_mock.Group, _mock.Master.UserId);
            var fieldView = vm.Fields.Field(field);

            fieldView.ShouldNotBeNull();
            fieldView.Value.ShouldBeNull();
            fieldView.ShouldBeHidden();
            fieldView.ShouldBeEditable();

        }

        [Fact]
        public void AddCharacterViewModelWithoutConditionalField()
        {
            var field = _mock.CreateConditionalField(new ProjectField(), _mock.Group);

            var groupForClaim = _mock.CreateCharacterGroup(new CharacterGroup());
            var vm = new AddCharacterViewModel().Fill(groupForClaim, _mock.Master.UserId);
            var fieldView = vm.Fields.Field(field);

            fieldView.ShouldNotBeNull();
            fieldView.Value.ShouldBeNull();
            fieldView.ShouldBeHidden();
            fieldView.ShouldBeReadonly();

        }

    }
}
