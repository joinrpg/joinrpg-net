using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.TestHelpers;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Money;
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

        [Fact]
        public void FinanceState()
            => EnumerationTestHelper.CheckEnums<FinanceOperationState, FinanceOperationStateViewModel>();

        [Fact]
        public void MoneyTransferState()
            => EnumerationTestHelper.CheckEnums<MoneyTransferState, MoneyTransferStateViewModel>();

        [Fact]
        public void ProjectType()
            => EnumerationTestHelper.CheckEnums<ProjectTypeDto, ProjectTypeViewModel>();

        [Fact]
        public void ContactsAccessType()
            => EnumerationTestHelper.CheckEnums<ContactsAccessType, ContactsAccessTypeView>();
    }
}
