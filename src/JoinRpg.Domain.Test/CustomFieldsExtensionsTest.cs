using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using Shouldly;
using Xunit;

namespace JoinRpg.Domain.Test;

public class CustomFieldsExtensionsTest
{
    private MockedProject projectMock = new();
    private ProjectFieldInfo[] allFieldsExceptMasterOnly;
    private ProjectFieldInfo[] allFields;

    public CustomFieldsExtensionsTest()
    {
        var conditionalHeader = projectMock.CreateConditionalHeader(projectMock.Group);
        var conditionalField = projectMock.CreateConditionalField();

        allFieldsExceptMasterOnly = projectMock.ProjectInfo.UnsortedFields.Except(new[] { projectMock.MasterOnlyFieldInfo }).ToArray();

        allFields = projectMock.ProjectInfo.UnsortedFields.ToArray();
    }

    [Fact]
    public void CharacterFieldVisibilityByPlayerTest()
    {
        VerifyCharacter( //Assert that
          projectMock.Player, //a player user can see only public fields of a character
          projectMock.PublicFieldInfo);
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
          projectMock.ProjectInfo,
          allFieldsExceptMasterOnly);
    }

    [Fact]
    public void NotApprovedClaimFieldVisibilitysTest()
    {
        //Ensure that
        VerifyClaim(
          projectMock.CreateClaim(projectMock.Character, projectMock.Player), //when claim is not yet approved
          projectMock.Player, //then the user who created the claim can see only the fields below
          projectMock.ProjectInfo,
          projectMock.PublicFieldInfo);
    }

    [Fact]
    public void ApprovedClaimFieldVisibilitysByMasterTest()
    {
        VerifyClaim( //Ensure that
          projectMock.CreateApprovedClaim(projectMock.Character, projectMock.Player), //when claim is approved
          projectMock.Master, //then a Master sees every field
          projectMock.ProjectInfo,
          [.. projectMock.ProjectInfo.UnsortedFields]);
    }

    [Fact]
    public void ApprovedClaimFieldVisibilitysByAnotherPlayerTest()
    {
        var anotherPlayer = new User()
        {
            UserId = 3,
            PrefferedName = "Player2",
            Email = "player2@example.com",
        };

        VerifyClaim( //Ensure that
          projectMock.CreateApprovedClaim(projectMock.Character, projectMock.Player), //when claim is approved
          anotherPlayer, //then other users see ony public info
          projectMock.ProjectInfo,
          projectMock.PublicFieldInfo);
    }

    [Fact]
    public void NotApprovedClaimFieldVisibilitysByMasterTest()
    {
        VerifyClaim( //Ensure that
          projectMock.CreateClaim(projectMock.Character, projectMock.Player), //when claim is not yet approved
          projectMock.Master, //then a Master sees every field
          projectMock.ProjectInfo,
          allFields);
    }

    private void VerifyClaim(Claim claim, User viewerUser, ProjectInfo projectInfo, params ProjectFieldInfo[] expectedFields)
    {
        var accessPredicate = claim.GetAccessArguments(viewerUser.UserId);

        var userVisibleFields = claim.GetFields(projectInfo).Where(f => f.Field.HasViewAccess(accessPredicate)).ToList();

        AssertCorrectFieldsArePresent(userVisibleFields, expectedFields);
    }

    private void VerifyCharacter(User viewerUser, params ProjectFieldInfo[] expectedFields)
    {
        var accessPredicate = projectMock.Character.GetAccessArguments(viewerUser.UserId);

        IList<FieldWithValue> userVisibleFields = projectMock.Character.GetFields(projectMock.ProjectInfo)
            .Where(f => f.Field.HasViewAccess(accessPredicate)).ToList();

        AssertCorrectFieldsArePresent(userVisibleFields, expectedFields);
    }

    private void AssertCorrectFieldsArePresent(IList<FieldWithValue> actualFields, params ProjectFieldInfo[] expectedFields) => actualFields.Select(actual => actual.Field).ShouldBe(expectedFields, ignoreOrder: true);
}
