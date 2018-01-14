using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using Shouldly; using Xunit;

namespace JoinRpg.Domain.Test
{
  
  public class CustomFieldsExtensionsTest
  {
    private MockedProject projectMock;
    private ProjectField[] allFieldsExceptMasterOnly;
    private ProjectField[] allFields;

      public CustomFieldsExtensionsTest()
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

    [Fact]
    public void CharacterFieldVisibilityByPlayerTest()
    {
      VerifyCharacter( //Assert that
        projectMock.Player, //a player user can see only public fields of a character
        projectMock.PublicField);
    }

    [Fact]
    public void CharacterFieldVisibilityByMasterTest()
    {
      VerifyCharacter( //Assert that
        projectMock.Master, //a master user can see every field of a character
        allFields);
    }

    [Fact]
    public void ApprovedClaimFieldVisibilitysTest()
    {
      VerifyClaim( //Ensure that
        projectMock.CreateApprovedClaim(projectMock.Character, projectMock.Player), //when claim is approved
        projectMock.Player, //then the user who created the claim can see only the fields below
        allFieldsExceptMasterOnly);
    }

    [Fact]
    public void NotApprovedClaimFieldVisibilitysTest()
    {
      //Ensure that
      VerifyClaim(
        projectMock.CreateClaim(projectMock.Character, projectMock.Player), //when claim is not yet approved
        projectMock.Player, //then the user who created the claim can see only the fields below
        projectMock.PublicField);
    }

   [Fact]
    public void ApprovedClaimFieldVisibilitysByMasterTest()
    {
      VerifyClaim( //Ensure that
        projectMock.CreateApprovedClaim(projectMock.Character, projectMock.Player), //when claim is approved
        projectMock.Master, //then a Master sees every field
        allFields);
    }

    [Fact]
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

    [Fact]
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
        expectedFields.Length.ShouldBe(actualFields.Count, $"Expected {expectedFields.Length} fields visible but were {actualFields.Count}");

        foreach (var expectedField in expectedFields)
        {
            actualFields.Any(f => f.Field.ProjectFieldId == expectedField.ProjectFieldId)
                .ShouldBeTrue(
                    $"The field {expectedField.FieldName} was expected to be visible but was not.");
        }
    }
  }
}
