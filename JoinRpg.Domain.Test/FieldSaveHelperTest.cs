using System;
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
    [TestMethod]
    public void SaveOnAddTest()
    {
      var mock = new MockedProject();
      var savedData = mock.Character.JsonData;
      var savedGroups = mock.Character.Groups.ToList();
      var claim = mock.CreateClaim(mock.Character, mock.Player);
      // ReSharper disable once MustUseReturnValue
      FieldSaveHelper.SaveCharacterFieldsImpl(
        mock.Player.UserId, 
        null, 
        claim,
        new Dictionary<int, string>()
        {
          {mock.CharacterField.ProjectFieldId, "test"}
        });
      Assert.AreEqual(savedData, mock.Character.JsonData, "Adding claim should not modify any character fields");
      CollectionAssert.AreEqual(
        savedGroups, 
        mock.Character.Groups.ToList(),
        "Adding claim should not modify any character groups");
      Assert.AreEqual($"{{\"{mock.CharacterField.ProjectFieldId}\":\"test\"}}", claim.JsonData);
    }

    [TestMethod]
    [ExpectedException(typeof(NoAccessToProjectException))]
    public void TryToChangeMasterOnlyFieldOnAdd()
    {
      var mock = new MockedProject();
      // ReSharper disable once MustUseReturnValue
      FieldSaveHelper.SaveCharacterFieldsImpl(
        mock.Player.UserId,
        null,
        mock.CreateClaim(mock.Character, mock.Player),
        new Dictionary<int, string>()
        {
          {mock.MasterOnlyField.ProjectFieldId, "test"}
        });
    }

    [TestMethod]
    public void ApprovedClaimChangeTest()
    {
      var mock = new MockedProject();
      var claim = mock.CreateClaim(mock.Character, mock.Player);
      claim.ClaimStatus = Claim.Status.Approved;
      // ReSharper disable once MustUseReturnValue
      FieldSaveHelper.SaveCharacterFieldsImpl(
        mock.Player.UserId,
        mock.Character,
        claim,
        new Dictionary<int, string>()
        {
          {mock.CharacterField.ProjectFieldId, "test"}
        });
      Assert.AreEqual($"{{\"{mock.CharacterField.ProjectFieldId}\":\"test\"}}", mock.Character.JsonData);
      Assert.AreEqual("{}", claim.JsonData);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void DisableUnapprovedClaimToChangeCharacterTest()
    {
      var mock = new MockedProject();
      // ReSharper disable once MustUseReturnValue
      FieldSaveHelper.SaveCharacterFieldsImpl(
        mock.Player.UserId,
        mock.Character,
        mock.CreateClaim(mock.Character, mock.Player),
        new Dictionary<int, string>()
        {
          {mock.CharacterField.ProjectFieldId, "test"}
        });
    }

    [TestMethod]
    [ExpectedException(typeof(NoAccessToProjectException))]
    public void TryToChangeAnotherUserCharacter()
    {
      var mock = new MockedProject();
      var claim = mock.CreateClaim(mock.Character, mock.Master);
      claim.ClaimStatus = Claim.Status.Approved;
      // ReSharper disable once MustUseReturnValue
      FieldSaveHelper.SaveCharacterFieldsImpl(
        mock.Player.UserId,
        mock.Character,
        claim,
        new Dictionary<int, string>()
        {
          {mock.CharacterField.ProjectFieldId, "test"}
        });
    }
  }
}
