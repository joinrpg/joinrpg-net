using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.TestHelpers;
using JoinRpg.Web.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JoinRpg.Web.Test
{
    [TestClass]
    public class EnumTests
    {
        [TestMethod]
        public void ProblemEnum()
        {
            EnumerationTestHelper.CheckEnums<UserExtensions.AccessReason, AccessReason>();
            EnumerationTestHelper.CheckEnums<ProjectFieldViewType, DataModel.ProjectFieldType>();
            EnumerationTestHelper.CheckEnums<ClaimStatusView, DataModel.Claim.Status>();
            EnumerationTestHelper.CheckEnums<FinanceOperationActionView, FinanceOperationAction>();
        }
    }
}
