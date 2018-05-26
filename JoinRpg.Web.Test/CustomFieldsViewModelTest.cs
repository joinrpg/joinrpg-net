using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.Web.Models;
using Shouldly; using Xunit;

namespace JoinRpg.Web.Test
{
  
  public class CustomFieldsViewModelTest
  {
    private static MockedProject Mock { get; } = new MockedProject();

      [Fact]
    public void HideMasterOnlyFieldOnAddClaimTest()
    {
      var vm = new CustomFieldsViewModel(Mock.Player.UserId, Mock.Group);
      (vm.FieldById(Mock.MasterOnlyField.ProjectFieldId)?.CanView ?? false).ShouldBeFalse();
    }

    [Fact]
    public void HideUnApprovedFieldOnAddClaimTest()
    {
      var vm = new CustomFieldsViewModel(Mock.Player.UserId, Mock.Group);
      (vm.FieldById(Mock.HideForUnApprovedClaim.ProjectFieldId)?.CanView ?? false).ShouldBeFalse();
    }

      [Fact]
      public void ShowPublicFieldToAnon()
      {
          MockedProject.AssignFieldValues(Mock.Character,
              new FieldWithValue(Mock.PublicField, "1"));

          var vm = new CustomFieldsViewModel(currentUserId: null, character: Mock.Character);

          var publicField = vm.FieldById(Mock.PublicField.ProjectFieldId);
          publicField.ShouldNotBeNull();
          publicField.CanView.ShouldBeTrue();
          publicField.Value.ShouldNotBeNull();
      }

      [Fact]
    public void ShowPublicFieldToAnonEvenIfEditDisabled()
    {
        MockedProject.AssignFieldValues(Mock.Character, new FieldWithValue(Mock.PublicField, "1"));

            var vm = new CustomFieldsViewModel(currentUserId: null, character: Mock.Character, disableEdit: true);
        var publicField = vm.FieldById(Mock.PublicField.ProjectFieldId);
        publicField.ShouldNotBeNull();
        publicField.CanView.ShouldBeTrue();
    }


    [Fact]  
    public void AllowCharactersFieldOnAddClaimTest()
    {
      var vm = new CustomFieldsViewModel(Mock.Player.UserId, (IClaimSource) Mock.Character);
      var characterField = vm.FieldById(Mock.CharacterField.ProjectFieldId);

        characterField.ShouldNotBeNull();
      characterField.CanView.ShouldBeFalse();
      characterField.Value.ShouldBeNull();

      characterField.CanEdit.ShouldBeTrue();
    }

    [Fact]
    public void AllowShadowCharacterFieldsTest()
    {
      var mock = new MockedProject();
      var claim = mock.CreateClaim(mock.Character, mock.Player);
        MockedProject.AssignFieldValues(claim, new FieldWithValue(mock.CharacterField, "test"));

      var vm = new CustomFieldsViewModel(mock.Player.UserId, claim);

      var characterField = vm.FieldById(mock.CharacterField.ProjectFieldId);

        characterField.ShouldNotBeNull();
        characterField.ShouldBeHidden();
        characterField.Value.ShouldBe("test");

            characterField.ShouldBeEditable();
    }

    [Fact]
    public void DoNotDiscloseOriginalFieldValuesTest()
    {
      var mock = new MockedProject();
      var claim = mock.CreateClaim(mock.Character, mock.Player);

      var vm = new CustomFieldsViewModel(mock.Player.UserId, claim);

      var characterField = vm.Field(mock.CharacterField);

        characterField.ShouldNotBeNull();
        characterField.ShouldBeHidden();
        characterField.ShouldNotHaveValue();

        characterField.ShouldBeEditable();
        }

      [Fact]
      public void DoNotDiscloseOriginalFieldValuesOnAddTest()
      {
          var mock = new MockedProject();

          var vm = new CustomFieldsViewModel(mock.Player.UserId, (IClaimSource)mock.Character);

          var characterField = vm.Field(mock.CharacterField);

          characterField.ShouldNotBeNull();
          characterField.ShouldBeHidden();
          characterField.ShouldNotHaveValue();

          characterField.ShouldBeEditable();
      }


        [Fact]
    public void AllowCharactersFieldOnAddClaimForCharacterTest()
    {
      var vm = new CustomFieldsViewModel(Mock.Player.UserId, (IClaimSource)Mock.Character);
      var characterField = vm.Field(Mock.CharacterField);
        characterField.ShouldNotBeNull();

        characterField.ShouldBeHidden();
        characterField.ShouldNotHaveValue();

        characterField.ShouldBeEditable();
        }

      [Fact]
      public void ProperlyHideConditionalHeader()
      {
          var conditionalHeader = Mock.CreateConditionalHeader();
          var claim = Mock.CreateApprovedClaim(Mock.CharacterWithoutGroup, Mock.Player);

          var vm = new CustomFieldsViewModel(Mock.Player.UserId, claim);
          var characterField = vm.FieldById(conditionalHeader.ProjectFieldId);

          characterField.ShouldNotBeNull();
          characterField.ShouldBeHidden();
          characterField.ShouldNotHaveValue();

          characterField.ShouldBeReadonly();
      }

      [Fact]
      public void ProperlyShowConditionalHeaderTest()
      {
          var conditionalHeader = Mock.CreateConditionalHeader();
          var claim = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);

          var vm = new CustomFieldsViewModel(Mock.Player.UserId, claim);
          var characterField = vm.FieldById(conditionalHeader.ProjectFieldId);

          characterField.ShouldNotBeNull();
          characterField.ShouldBeVisible();
          characterField.ShouldNotHaveValue();

          characterField.ShouldBeEditable();
      }


      [Fact]
      public void AllowCharactersFieldOnAddClaimForGroupTest()
      {
          var vm = new CustomFieldsViewModel(Mock.Player.UserId, Mock.Group);
          var characterField = vm.FieldById(Mock.CharacterField.ProjectFieldId);
          characterField.ShouldNotBeNull();
          characterField.ShouldBeHidden();

          characterField.ShouldNotHaveValue();

          characterField.ShouldBeEditable();
      }

      [Fact]
      public void ShowPublicCharactersFieldValueOnAddClaim()
      {
          var field = Mock.CreateField(new ProjectField() {IsPublic = true, CanPlayerEdit = false});
          var value = new FieldWithValue(field, "xxx");
          Mock.Character.JsonData = new[] {value}.SerializeFields();

          var vm = new CustomFieldsViewModel(Mock.Player.UserId, (IClaimSource) Mock.Character);
          var characterField = vm.Field(field);
          characterField.ShouldNotBeNull();
          characterField.ShouldBeVisible();

          characterField.Value.ShouldBe("xxx");

          characterField.ShouldBeReadonly();
      }

      [Fact]
      public void PublicCharactersFieldValueOnAddClaimAreReadonlyIfNotShowForUnApprovedClaims()
      {
          var field = Mock.CreateField(new ProjectField() { IsPublic = true, CanPlayerEdit = true, ShowOnUnApprovedClaims = false});
          var value = new FieldWithValue(field, "xxx");
          Mock.Character.JsonData = new[] { value }.SerializeFields();

          var vm = new CustomFieldsViewModel(Mock.Player.UserId, (IClaimSource)Mock.Character);
          var characterField = vm.Field(field);
          characterField.ShouldNotBeNull();
          characterField.ShouldBeVisible();

          characterField.Value.ShouldBe("xxx");

          characterField.ShouldBeReadonly();
      }

      [Fact]
      public void PublicCharactersFieldValueOnAddClaimShouldBeEditableIfNotShowForUnApprovedClaims()
      {
          var field = Mock.CreateField(new ProjectField()
          {
              IsPublic = true,
              CanPlayerEdit = true,
              ShowOnUnApprovedClaims = true,
          });
          var value = new FieldWithValue(field, "xxx");
          Mock.Character.JsonData = new[] { value }.SerializeFields();

          var vm = new CustomFieldsViewModel(Mock.Player.UserId, (IClaimSource)Mock.Character);
          var characterField = vm.Field(field);
          characterField.ShouldNotBeNull();
          characterField.ShouldBeVisible();

          characterField.Value.ShouldBe("xxx");

          characterField.ShouldBeEditable();
      }
    }
}
