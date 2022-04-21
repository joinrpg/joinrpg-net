using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.TestHelpers;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Money;
using Xunit;

namespace JoinRpg.Web.Test;

public class EnumTests
{
    [Fact]
    public void AccessReason() => EnumerationTestComparer.EnsureSame<UserExtensions.AccessReason, AccessReason>();

    [Fact]
    public void ProjectFieldType() => EnumerationTestComparer.EnsureSame<ProjectFieldViewType, DataModel.ProjectFieldType>();

    [Fact]
    public void ClaimStatus() => EnumerationTestComparer.EnsureSame<ClaimStatusView, DataModel.Claim.Status>();

    [Fact]
    public void ClaimDenialStatus() => EnumerationTestComparer.EnsureSame<ClaimDenialStatusView, DataModel.Claim.DenialStatus>();

    [Fact]
    public void FinanceOperation() => EnumerationTestComparer.EnsureSame<FinanceOperationActionView, FinanceOperationAction>();

    [Fact]
    public void FinanceState()
        => EnumerationTestComparer.EnsureSame<FinanceOperationState, FinanceOperationStateViewModel>();

    [Fact]
    public void MoneyTransferState()
        => EnumerationTestComparer.EnsureSame<MoneyTransferState, MoneyTransferStateViewModel>();

    [Fact]
    public void ProjectType()
        => EnumerationTestComparer.EnsureSame<ProjectTypeDto, ProjectTypeViewModel>();

    [Fact]
    public void ContactsAccessType()
        => EnumerationTestComparer.EnsureSame<ContactsAccessType, ContactsAccessTypeView>();
}
