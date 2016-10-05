using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Web.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JoinRpg.Web.Test
{
  [TestClass]
  public class CustomFieldsViewModelTest
  {
    private MockedProject Mock { get; } = new MockedProject();

    [TestMethod]
    public void HideMasterOnlyFieldOnAddClaimTest()
    {
      var vm = new CustomFieldsViewModel(Mock.Player.UserId, Mock.Group);
      Assert.IsFalse(vm.FieldById(Mock.MasterOnlyField.ProjectFieldId)?.CanView ?? false);
    }

    [TestMethod]
    public void HideUnApprovedFieldOnAddClaimTest()
    {
      var vm = new CustomFieldsViewModel(Mock.Player.UserId, Mock.Group);
      Assert.IsFalse(vm.FieldById(Mock.HideForUnApprovedClaim.ProjectFieldId)?.CanView ?? false);
    }

    [TestMethod]
    public void AllowCharactersFieldOnAddClaimTest()
    {
      var vm = new CustomFieldsViewModel(Mock.Player.UserId, (IClaimSource) Mock.Character);
      var characterField = vm.FieldById(Mock.CharacterField.ProjectFieldId);
      Assert.IsNotNull(characterField);
      Assert.IsFalse(characterField.CanView);
      Assert.IsNull(characterField.Value);

      Assert.IsTrue(characterField.CanEdit);
    }

    [TestMethod]
    public void AllowShadowCharacterFieldsTest()
    {
      var mock = new MockedProject();
      var claim = mock.CreateClaim(mock.Character, mock.Player);
      claim.JsonData = $"{{\"{mock.CharacterField.ProjectFieldId}\":\"test\"}}";

      var vm = new CustomFieldsViewModel(mock.Player.UserId, claim);

      var characterField = vm.FieldById(mock.CharacterField.ProjectFieldId);

      Assert.IsNotNull(characterField);
      Assert.IsFalse(characterField.CanView);
      Assert.AreEqual("test", characterField.Value);

      Assert.IsTrue(characterField.CanEdit);
    }

    [TestMethod]
    public void DoNotDiscloseOriginalFieldValuesTest()
    {
      var mock = new MockedProject();
      var claim = mock.CreateClaim(mock.Character, mock.Player);

      var vm = new CustomFieldsViewModel(mock.Player.UserId, claim);

      var characterField = vm.FieldById(mock.CharacterField.ProjectFieldId);

      Assert.IsNotNull(characterField);
      Assert.IsFalse(characterField.CanView);
      Assert.IsNull(characterField.Value);

      Assert.IsTrue(characterField.CanEdit);
    }

    [TestMethod]
    public void AllowCharactersFieldOnAddClaimForCharacterTest()
    {
      var vm = new CustomFieldsViewModel(Mock.Player.UserId, (IClaimSource)Mock.Character);
      var characterField = vm.FieldById(Mock.CharacterField.ProjectFieldId);
      Assert.IsNotNull(characterField);
      Assert.IsFalse(characterField.CanView);
      Assert.IsNull(characterField.Value);

      Assert.IsTrue(characterField.CanEdit);
    }
    [TestMethod]
    public void AllowCharactersFieldOnAddClaimForGroupTest()
    {
      var vm = new CustomFieldsViewModel(Mock.Player.UserId, Mock.Group);
      var characterField = vm.FieldById(Mock.CharacterField.ProjectFieldId);
      Assert.IsNotNull(characterField);
      Assert.IsFalse(characterField.CanView);
      Assert.IsNull(characterField.Value);

      Assert.IsTrue(characterField.CanEdit);
    }
  }
}
