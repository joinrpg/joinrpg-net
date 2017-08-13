using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain.CharacterFields;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JoinRpg.Domain.Test
{
  [TestClass]
  public class FieldSaveHelperTest
  {
    private MockedProject _original;
    private IFieldDefaultValueGenerator generator;

    [TestInitialize]
    public void SetUp()
    {
      _original = new MockedProject();
      generator = new MockedFieldDefaultValueGenerator();
    }

    [TestMethod]
    public void SaveOnAddTest()
    {
      var mock = new MockedProject();
      var claim = mock.CreateClaim(mock.Character, mock.Player);
      // ReSharper disable once MustUseReturnValue
      FieldSaveHelper.SaveCharacterFields(
        mock.Player.UserId, 
        claim,
        new Dictionary<int, string>()
        {
          {mock.CharacterField.ProjectFieldId, "test"}
        },
        generator);
      Assert.AreEqual(_original.Character.JsonData, mock.Character.JsonData, "Adding claim should not modify any character fields");
      CollectionAssert.AreEqual(
        _original.Character.Groups.Select(g => g.CharacterGroupId).ToList(), 
        mock.Character.Groups.Select(g => g.CharacterGroupId).ToList(),
        "Adding claim should not modify any character groups");
      Assert.AreEqual($"{{\"{mock.CharacterField.ProjectFieldId}\":\"test\"}}", claim.JsonData);
    }

    [TestMethod]
    [ExpectedException(typeof(NoAccessToProjectException))]
    public void TryToChangeMasterOnlyFieldOnAdd()
    {
      var mock = new MockedProject();
      FieldSaveHelper.SaveCharacterFields(
        mock.Player.UserId,
        mock.CreateClaim(mock.Character, mock.Player),
        new Dictionary<int, string>()
        {
          {mock.MasterOnlyField.ProjectFieldId, "test"}
        },
        generator);
    }

    [TestMethod]
    public void ApprovedClaimHiddenChangeTest()
    {
      var mock = new MockedProject();
      var claim = mock.CreateApprovedClaim(mock.Character, mock.Player);
      FieldSaveHelper.SaveCharacterFields(
        mock.Player.UserId,
        claim,
        new Dictionary<int, string>()
        {
          {mock.HideForUnApprovedClaim.ProjectFieldId, "test"},
          {mock.CharacterField.ProjectFieldId, null }
        },
        generator);
      Assert.AreEqual(
  $"{{\"{mock.HideForUnApprovedClaim.ProjectFieldId}\":\"test\",\"{mock.PublicField.ProjectFieldId}\":\"Public\"}}",
  mock.Character.JsonData);
      Assert.AreEqual("{}", claim.JsonData);
    }

    [TestMethod]
    public void MasterHiddenChangeTest()
    {
      var mock = new MockedProject();
      FieldSaveHelper.SaveCharacterFields(
        mock.Master.UserId,
        mock.Character,
        new Dictionary<int, string>()
        {
          {mock.HideForUnApprovedClaim.ProjectFieldId, "test"},
          {mock.CharacterField.ProjectFieldId, null }
        },
        generator);
      Assert.AreEqual($"{{\"{mock.HideForUnApprovedClaim.ProjectFieldId}\":\"test\",\"{mock.PublicField.ProjectFieldId}\":\"Public\"}}", mock.Character.JsonData);
    }

    [TestMethod]
    public void ApprovedClaimChangeTest()
    {
      var mock = new MockedProject();
      var claim = mock.CreateClaim(mock.Character, mock.Player);
      claim.ClaimStatus = Claim.Status.Approved;
      mock.Character.ApprovedClaim = claim;

      FieldSaveHelper.SaveCharacterFields(
        mock.Player.UserId,
        claim,
        new Dictionary<int, string>()
        {
          {mock.CharacterField.ProjectFieldId, "test"}
        },
        generator);
      Assert.AreEqual(
        $"{{\"{mock.CharacterField.ProjectFieldId}\":\"test\",\"{mock.PublicField.ProjectFieldId}\":\"Public\"}}",
        mock.Character.JsonData);
      Assert.AreEqual("{}", claim.JsonData);
    }

    [TestMethod]
    public void ConditionalFieldChangeTest()
    {
      var mock = new MockedProject();
      var claim = mock.CreateClaim(mock.Character, mock.Player);
     
      FieldSaveHelper.SaveCharacterFields(
        mock.Player.UserId,
        claim,
        new Dictionary<int, string>()
        {
          {mock.ConditionalField.ProjectFieldId, "test"}
        },
        generator);
      Assert.AreEqual($"{{\"{mock.ConditionalField.ProjectFieldId}\":\"test\"}}", claim.JsonData);
      Assert.AreEqual(_original.Character.JsonData, mock.Character.JsonData,
        "Adding claim should not modify any character fields");
    }


    [TestMethod]
    public void ConditionalFieldChangeTestForGroup()
    {
      var mock = new MockedProject();
      var claim = mock.CreateClaim(mock.Group, mock.Player);

      FieldSaveHelper.SaveCharacterFields(
        mock.Player.UserId,
        claim,
        new Dictionary<int, string>()
        {
          {mock.ConditionalField.ProjectFieldId, "test"}
        },
        generator);
      Assert.AreEqual($"{{\"{mock.ConditionalField.ProjectFieldId}\":\"test\"}}", claim.JsonData);
      Assert.AreEqual(_original.Character.JsonData, mock.Character.JsonData,
        "Adding claim should not modify any character fields");
    }


    [TestMethod]
    [ExpectedException(typeof(NoAccessToProjectException))]
    public void HiddenFieldChangeFailedTest()
    {
      var mock = new MockedProject();
      var claim = mock.CreateClaim(mock.Group, mock.Player);

      FieldSaveHelper.SaveCharacterFields(
        mock.Player.UserId,
        claim,
        new Dictionary<int, string>()
        {
          {mock.HideForUnApprovedClaim.ProjectFieldId, "test"}
        },
        generator);
    }

    [TestMethod]
    public void DisableUnapprovedClaimToChangeCharacterTest()
    {
      var mock = new MockedProject();
      var claim = mock.CreateClaim(mock.Character, mock.Player);
      FieldSaveHelper.SaveCharacterFields(
        mock.Player.UserId,
        claim,
        new Dictionary<int, string>()
        {
          {mock.CharacterField.ProjectFieldId, "test"}
        },
        generator);
      Assert.AreEqual(_original.Character.JsonData, mock.Character.JsonData, "Adding claim should not modify any character fields");
      CollectionAssert.AreEqual(
        _original.Character.Groups.Select(g => g.CharacterGroupId).ToList(),
        mock.Character.Groups.Select(g => g.CharacterGroupId).ToList(),
        "Adding claim should not modify any character groups");
      Assert.AreEqual($"{{\"{mock.CharacterField.ProjectFieldId}\":\"test\"}}", claim.JsonData);
    }

    [TestMethod]
    [ExpectedException(typeof(NoAccessToProjectException))]
    public void TryToChangeAnotherUserCharacter()
    {
      var mock = new MockedProject();
      FieldSaveHelper.SaveCharacterFields(
        mock.Player.UserId,
        mock.Character,
        new Dictionary<int, string>()
        {
          {mock.CharacterField.ProjectFieldId, "test"}
        },
        generator);
    }
  }

  public class MockedFieldDefaultValueGenerator : IFieldDefaultValueGenerator
  {
    public string CreateDefaultValue(Claim claim, ProjectField feld) => null;
    public string CreateDefaultValue(Character character, ProjectField field) => null;
  }
}
