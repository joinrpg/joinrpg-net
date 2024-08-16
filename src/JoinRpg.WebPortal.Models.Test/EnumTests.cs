using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.TestHelpers;
using JoinRpg.Web.Claims.Finance;
using JoinRpg.Web.Models;
using MoreLinq;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Test;

public class EnumTests
{
    [Fact]
    public void AccessReason() => EnumerationTestComparer.EnsureSame<UserExtensions.AccessReason, AccessReason>();

    [Fact]
    public void ProjectFieldType() => EnumerationTestComparer.EnsureSame<ProjectFieldViewType, ProjectFieldType>();

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

    [Theory]
    [ClassData(typeof(EnumTheoryDataGenerator<ProjectFieldViewType>))]
    public void FieldViewTypeHasCorrectDisplay(ProjectFieldViewType projectFieldType)
    {
        var displayAttribute = projectFieldType.GetAttribute<DisplayAttribute>();
        displayAttribute.ShouldNotBeNull();
        displayAttribute.Name.ShouldNotBeNull();
        displayAttribute.Order.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void FieldViewTypeHasDisctinctOrder()
    {
        Enum.GetValues<ProjectFieldViewType>().Select(pft => pft.GetAttribute<DisplayAttribute>()?.Order).Duplicates().ShouldBeEmpty();
    }
}
