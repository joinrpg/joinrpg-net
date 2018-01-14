using JoinRpg.Domain;
using JoinRpg.TestHelpers;
using JoinRpg.Web.Models;
using Xunit;

namespace JoinRpg.Web.Test
{
  
  public class EnumTests
  {
    [Fact]
    public void ProblemEnum()
    {
      EnumerationTestHelper.CheckEnums<UserExtensions.AccessReason, AccessReason>();
      EnumerationTestHelper.CheckEnums<ProjectFieldViewType, DataModel.ProjectFieldType>();
      EnumerationTestHelper.CheckEnums<ClaimStatusView, DataModel.Claim.Status>();
    }
  }
}
