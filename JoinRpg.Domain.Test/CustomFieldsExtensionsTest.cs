using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JoinRpg.Domain.Test
{
  [TestClass]
  public class CustomFieldsExtensionsTest
  {
    private MockedProject projectMock;
    private ProjectField[] allFieldsExceptMasterOnly;
    private ProjectField[] allFields;
    [TestInitialize]
    public void Initialize()
    {
      projectMock = new MockedProject();
      allFieldsExceptMasterOnly = new[] 
      {
        projectMock.CharacterField,
        projectMock.PublicField,
        projectMock.HideForUnApprovedClaim,
        projectMock.ConditionalField,
        projectMock.ConditionalHeader
      };

      allFields = allFieldsExceptMasterOnly
        .Union(new[] {projectMock.MasterOnlyField})
        .ToArray();
    }

    [TestMethod]
    public void CharacterFieldVisibilityByPlayerTest()
    {
      VerifyCharacter( //Assert that
        projectMock.Player, //a player user can see only public fields of a character
        projectMock.PublicField);
    }

    [TestMethod]
    public void CharacterFieldVisibilityByMasterTest()
    {
      VerifyCharacter( //Assert that
        projectMock.Master, //a master user can see every field of a character
        allFields);
    }

    [TestMethod]
    public void ApprovedClaimFieldVisibilitysTest()
    {
      VerifyClaim( //Ensure that
        projectMock.CreateApprovedClaim(projectMock.Character, projectMock.Player), //when claim is approved
        projectMock.Player, //then the user who created the claim can see only the fields below
        allFieldsExceptMasterOnly);
    }

    [TestMethod]
    public void NotApprovedClaimFieldVisibilitysTest()
    {
      //Ensure that
      VerifyClaim(
        projectMock.CreateClaim(projectMock.Character, projectMock.Player), //when claim is not yet approved
        projectMock.Player, //then the user who created the claim can see only the fields below
        projectMock.PublicField);
    }

   [TestMethod]
    public void ApprovedClaimFieldVisibilitysByMasterTest()
    {
      VerifyClaim( //Ensure that
        projectMock.CreateApprovedClaim(projectMock.Character, projectMock.Player), //when claim is approved
        projectMock.Master, //then a Master sees every field
        allFields);
    }

    [TestMethod]
    public void ApprovedClaimFieldVisibilitysByAnotherPlayerTest()
    {
      var anotherPlayer = new User()
      {
        UserId = 3,
        PrefferedName = "Player2",
        Email = "player2@example.com"
      };

      VerifyClaim( //Ensure that
        projectMock.CreateApprovedClaim(projectMock.Character, projectMock.Player), //when claim is approved
        anotherPlayer, //then other users see ony public info
        projectMock.PublicField);
    }

    [TestMethod]
    public void NotApprovedClaimFieldVisibilitysByMasterTest()
    {
      VerifyClaim( //Ensure that
        projectMock.CreateClaim(projectMock.Character, projectMock.Player), //when claim is not yet approved
        projectMock.Master, //then a Master sees every field
        allFields);
    }

    private void VerifyClaim(Claim claim, User viewerUser, params ProjectField[] expectedFields)
    {
      var accessPredicate = CustomFieldsExtensions.GetShowForUserPredicate(claim, viewerUser.UserId);

      IList<FieldWithValue> userVisibleFields = claim.GetFields().Where(f => accessPredicate(f)).ToList();

      AssertCorrectFieldsArePresent(userVisibleFields, expectedFields);
    }

    private void VerifyCharacter(User viewerUser, params ProjectField[] expectedFields)
    {
      var accessPredicate = CustomFieldsExtensions.GetShowForUserPredicate(projectMock.Character, viewerUser.UserId);

      IList<FieldWithValue> userVisibleFields = projectMock.Character.GetFields().Where(f => accessPredicate(f)).ToList();

      AssertCorrectFieldsArePresent(userVisibleFields, expectedFields);
    }

    private void AssertCorrectFieldsArePresent(IList<FieldWithValue> actualFields, params ProjectField[] expectedFields)
    {
      Assert.AreEqual(actualFields.Count, expectedFields.Length,
        $"Expected {expectedFields.Length} fields visible but were {actualFields.Count}");

      foreach (var expectedField in expectedFields)
      {
        Assert.IsTrue(
          actualFields.Any(f => f.Field.ProjectFieldId == expectedField.ProjectFieldId),
          $"The field {expectedField.FieldName} was expected to be visible but was not.");
      }
    }
  }
}
