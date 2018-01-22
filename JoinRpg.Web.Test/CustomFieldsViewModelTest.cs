using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Web.Models;
using Shouldly; using Xunit;

namespace JoinRpg.Web.Test
{
  
  public class CustomFieldsViewModelTest
  {
    private MockedProject Mock { get; } = new MockedProject();

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
      var vm = new CustomFieldsViewModel(null, Mock.Character);
      var publicField = vm.FieldById(Mock.PublicField.ProjectFieldId);
      (publicField?.CanView ?? true).ShouldBeTrue();
      publicField?.Value.ShouldNotBeNull();
    }

    [Fact]
    public void ShowPublicFieldToAnonEvenIfEditDisabled()
    {
      var vm = new CustomFieldsViewModel(null, Mock.Character, disableEdit: true);
      (vm.FieldById(Mock.PublicField.ProjectFieldId)?.CanView ?? true).ShouldBeTrue();
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
      claim.JsonData = $"{{\"{mock.CharacterField.ProjectFieldId}\":\"test\"}}";

      var vm = new CustomFieldsViewModel(mock.Player.UserId, claim);

      var characterField = vm.FieldById(mock.CharacterField.ProjectFieldId);

        characterField.ShouldNotBeNull();
        characterField.CanView.ShouldBeFalse();
        characterField.Value.ShouldBe("test");
            characterField.CanEdit.ShouldBeTrue();
    }

    [Fact]
    public void DoNotDiscloseOriginalFieldValuesTest()
    {
      var mock = new MockedProject();
      var claim = mock.CreateClaim(mock.Character, mock.Player);

      var vm = new CustomFieldsViewModel(mock.Player.UserId, claim);

      var characterField = vm.FieldById(mock.CharacterField.ProjectFieldId);

        characterField.ShouldNotBeNull();
        characterField.CanView.ShouldBeFalse();
        characterField.Value.ShouldBeNull();

        characterField.CanEdit.ShouldBeTrue();
        }

    [Fact]
    public void AllowCharactersFieldOnAddClaimForCharacterTest()
    {
      var vm = new CustomFieldsViewModel(Mock.Player.UserId, (IClaimSource)Mock.Character);
      var characterField = vm.FieldById(Mock.CharacterField.ProjectFieldId);
        characterField.ShouldNotBeNull();
        characterField.CanView.ShouldBeFalse();
        characterField.Value.ShouldBeNull();

        characterField.CanEdit.ShouldBeTrue();
        }

    [Fact]
    public void ProperlyHideConditionalHeader()
    {
      var mock = new MockedProject();
      var claim = mock.CreateApprovedClaim(mock.CharacterWithoutGroup, mock.Player);

      var vm = new CustomFieldsViewModel(mock.Player.UserId, claim);
      var characterField = vm.FieldById(mock.ConditionalHeader.ProjectFieldId);

        characterField.ShouldNotBeNull();
        characterField.CanView.ShouldBeFalse();
        characterField.Value.ShouldBeNull();

        characterField.CanEdit.ShouldBeFalse();
        }

    [Fact]
    public void ProperlyShowConditionalHeaderTest()
    {
      var mock = new MockedProject();
      var claim = mock.CreateApprovedClaim(mock.Character, mock.Player);

      var vm = new CustomFieldsViewModel(mock.Player.UserId, claim);
      var characterField = vm.FieldById(mock.ConditionalHeader.ProjectFieldId);

        characterField.ShouldNotBeNull();
        characterField.CanView.ShouldBeTrue();
        characterField.Value.ShouldBeNull();

        characterField.CanEdit.ShouldBeTrue();
    }


    [Fact]
    public void AllowCharactersFieldOnAddClaimForGroupTest()
    {
      var vm = new CustomFieldsViewModel(Mock.Player.UserId, Mock.Group);
      var characterField = vm.FieldById(Mock.CharacterField.ProjectFieldId);
        characterField.ShouldNotBeNull();
        characterField.CanView.ShouldBeFalse();

        characterField.Value.ShouldBeNull();

        characterField.CanEdit.ShouldBeTrue();
        }
  }
}
