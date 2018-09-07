using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.TestHelpers;
using JoinRpg.Web.Models;
using Xunit;

namespace JoinRpg.Web.Test
{

    public class EnumTests
    {
        [Fact]
        public void AccessReason() => EnumerationTestHelper.CheckEnums<UserExtensions.AccessReason, AccessReason>();

        [Fact]
        public void ProjectFieldType() => EnumerationTestHelper.CheckEnums<ProjectFieldViewType, DataModel.ProjectFieldType>();

        [Fact]
        public void ClaimStatus() => EnumerationTestHelper.CheckEnums<ClaimStatusView, DataModel.Claim.Status>();

        [Fact]
        public void ClaimDenialStatus() => EnumerationTestHelper.CheckEnums<ClaimDenialStatusView, DataModel.Claim.DenialStatus>();

        [Fact]
        public void FinanceOperation() => EnumerationTestHelper.CheckEnums<FinanceOperationActionView, FinanceOperationAction>();
    }
}
