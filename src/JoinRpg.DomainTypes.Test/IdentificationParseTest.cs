using System.Text.Json;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.Claims;
using JoinRpg.DomainTypes.Forums;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;

namespace JoinRpg.DomainTypes.Test;

/// <summary>
/// Тесты парсинга всех типов идентификаторов.
/// Каждый тип поддерживает три формата: ShortName (каноничный), ShortName без Id, полное TypeName.
/// </summary>
public class IdentificationParseTest
{
    // ProjectIdentification — 1 листовой int
    [Theory]
    [InlineData("Project(1)")]
    [InlineData("ProjectIdentification(1)")]
    [InlineData("1")]
    public void ProjectShouldParse(string val)
    {
        ProjectIdentification.TryParse(val, provider: null, out var result).ShouldBeTrue();
        result.ShouldBe(new ProjectIdentification(1));
    }

    // ClaimIdentification — 2 листа: projectId, claimId
    [Theory]
    [InlineData("ClaimId(1-2)")]
    [InlineData("Claim(1-2)")]
    [InlineData("ClaimIdentification(1-2)")]
    [InlineData("1-2")]
    public void ClaimShouldParse(string val)
    {
        ClaimIdentification.TryParse(val, provider: null, out var result).ShouldBeTrue();
        result.ShouldBe(new ClaimIdentification(new ProjectIdentification(1), 2));
    }

    // CharacterIdentification — 2 листа: projectId, characterId
    [Theory]
    [InlineData("CharacterId(1-2)")]
    [InlineData("Character(1-2)")]
    [InlineData("CharacterIdentification(1-2)")]
    [InlineData("1-2")]
    public void CharacterShouldParse(string val)
    {
        CharacterIdentification.TryParse(val, provider: null, out var result).ShouldBeTrue();
        result.ShouldBe(new CharacterIdentification(new ProjectIdentification(1), 2));
    }

    // CharacterGroupIdentification — 2 листа: projectId, characterGroupId
    [Theory]
    [InlineData("CharacterGroupId(1-2)")]
    [InlineData("CharacterGroup(1-2)")]
    [InlineData("CharacterGroupIdentification(1-2)")]
    [InlineData("1-2")]
    public void CharacterGroupShouldParse(string val)
    {
        CharacterGroupIdentification.TryParse(val, provider: null, out var result).ShouldBeTrue();
        result.ShouldBe(new CharacterGroupIdentification(new ProjectIdentification(1), 2));
    }

    // ForumThreadIdentification — 2 листа: projectId, threadId
    [Theory]
    [InlineData("ForumThreadId(1-2)")]
    [InlineData("ForumThread(1-2)")]
    [InlineData("ForumThreadIdentification(1-2)")]
    [InlineData("1-2")]
    public void ForumThreadShouldParse(string val)
    {
        ForumThreadIdentification.TryParse(val, provider: null, out var result).ShouldBeTrue();
        result.ShouldBe(new ForumThreadIdentification(new ProjectIdentification(1), 2));
    }

    // PlotFolderIdentification — 2 листа: projectId, plotFolderId
    [Theory]
    [InlineData("PlotFolderId(1-2)")]
    [InlineData("PlotFolder(1-2)")]
    [InlineData("PlotFolderIdentification(1-2)")]
    [InlineData("1-2")]
    public void PlotFolderShouldParse(string val)
    {
        PlotFolderIdentification.TryParse(val, provider: null, out var result).ShouldBeTrue();
        result.ShouldBe(new PlotFolderIdentification(new ProjectIdentification(1), 2));
    }

    // PaymentTypeIdentification — 2 листа: projectId, paymentTypeId
    [Theory]
    [InlineData("PaymentTypeId(1-2)")]
    [InlineData("PaymentType(1-2)")]
    [InlineData("PaymentTypeIdentification(1-2)")]
    [InlineData("1-2")]
    public void PaymentTypeShouldParse(string val)
    {
        PaymentTypeIdentification.TryParse(val, provider: null, out var result).ShouldBeTrue();
        result.ShouldBe(new PaymentTypeIdentification(new ProjectIdentification(1), 2));
    }

    // ProjectFieldIdentification — 2 листа: projectId, projectFieldId
    [Theory]
    [InlineData("ProjectFieldId(1-2)")]
    [InlineData("ProjectField(1-2)")]
    [InlineData("ProjectFieldIdentification(1-2)")]
    [InlineData("1-2")]
    public void ProjectFieldShouldParse(string val)
    {
        ProjectFieldIdentification.TryParse(val, provider: null, out var result).ShouldBeTrue();
        result.ShouldBe(new ProjectFieldIdentification(new ProjectIdentification(1), 2));
    }

    // ClaimCommentIdentification — 3 листа: projectId, claimId, commentId
    [Theory]
    [InlineData("ClaimCommentId(1-2-3)")]
    [InlineData("ClaimComment(1-2-3)")]
    [InlineData("ClaimCommentIdentification(1-2-3)")]
    [InlineData("1-2-3")]
    public void ClaimCommentShouldParse(string val)
    {
        ClaimCommentIdentification.TryParse(val, provider: null, out var result).ShouldBeTrue();
        result.ShouldBe(new ClaimCommentIdentification(new ClaimIdentification(new ProjectIdentification(1), 2), 3));
    }

    // FinanceOperationIdentification — 3 листа: projectId, claimId, financeOperationId
    [Theory]
    [InlineData("FinanceOperationId(1-2-3)")]
    [InlineData("FinanceOperation(1-2-3)")]
    [InlineData("FinanceOperationIdentification(1-2-3)")]
    [InlineData("1-2-3")]
    public void FinanceOperationShouldParse(string val)
    {
        FinanceOperationIdentification.TryParse(val, provider: null, out var result).ShouldBeTrue();
        result.ShouldBe(new FinanceOperationIdentification(new ProjectIdentification(1), 2, 3));
    }

    // ForumCommentIdentification — 3 листа: projectId, threadId, commentId
    [Theory]
    [InlineData("ForumCommentId(1-2-3)")]
    [InlineData("ForumComment(1-2-3)")]
    [InlineData("ForumCommentIdentification(1-2-3)")]
    [InlineData("1-2-3")]
    public void ForumCommentShouldParse(string val)
    {
        ForumCommentIdentification.TryParse(val, provider: null, out var result).ShouldBeTrue();
        result.ShouldBe(new ForumCommentIdentification(new ForumThreadIdentification(new ProjectIdentification(1), 2), 3));
    }

    // PlotElementIdentification — 3 листа: projectId, plotFolderId, plotElementId
    [Theory]
    [InlineData("PlotElementId(1-2-3)")]
    [InlineData("PlotElement(1-2-3)")]
    [InlineData("PlotElementIdentification(1-2-3)")]
    [InlineData("1-2-3")]
    public void PlotElementShouldParse(string val)
    {
        PlotElementIdentification.TryParse(val, provider: null, out var result).ShouldBeTrue();
        result.ShouldBe(new PlotElementIdentification(new PlotFolderIdentification(new ProjectIdentification(1), 2), 3));
    }

    // ProjectFieldVariantIdentification — 3 листа: projectId, projectFieldId, projectFieldVariantId
    [Theory]
    [InlineData("ProjectFieldVariantId(1-2-3)")]
    [InlineData("ProjectFieldVariant(1-2-3)")]
    [InlineData("ProjectFieldVariantIdentification(1-2-3)")]
    [InlineData("1-2-3")]
    public void ProjectFieldVariantShouldParse(string val)
    {
        ProjectFieldVariantIdentification.TryParse(val, provider: null, out var result).ShouldBeTrue();
        result.ShouldBe(new ProjectFieldVariantIdentification(new ProjectFieldIdentification(new ProjectIdentification(1), 2), 3));
    }

    // PlotVersionIdentification — 4 листа: projectId, plotFolderId, plotElementId, version
    [Theory]
    [InlineData("PlotVersionId(1-2-3-4)")]
    [InlineData("PlotVersion(1-2-3-4)")]
    [InlineData("PlotVersionIdentification(1-2-3-4)")]
    [InlineData("1-2-3-4")]
    public void PlotVersionShouldParse(string val)
    {
        PlotVersionIdentification.TryParse(val, provider: null, out var result).ShouldBeTrue();
        result.ShouldBe(new PlotVersionIdentification(new PlotElementIdentification(new PlotFolderIdentification(new ProjectIdentification(1), 2), 3), 4));
    }

    // AccommodationRequestIdentification — 2 листа: projectId, accommodationRequestId
    [Theory]
    [InlineData("AccommodationRequestId(1-2)")]
    [InlineData("AccommodationRequest(1-2)")]
    [InlineData("AccommodationRequestIdentification(1-2)")]
    [InlineData("1-2")]
    public void AccommodationRequestShouldParse(string val)
    {
        AccommodationRequestIdentification.TryParse(val, provider: null, out var result).ShouldBeTrue();
        result.ShouldBe(new AccommodationRequestIdentification(new ProjectIdentification(1), 2));
    }

    // AccommodationTypeIdentification — 2 листа: projectId, accommodationTypeId
    [Theory]
    [InlineData("AccommodationTypeId(1-2)")]
    [InlineData("AccommodationType(1-2)")]
    [InlineData("AccommodationTypeIdentification(1-2)")]
    [InlineData("1-2")]
    public void AccommodationTypeShouldParse(string val)
    {
        AccommodationTypeIdentification.TryParse(val, provider: null, out var result).ShouldBeTrue();
        result.ShouldBe(new AccommodationTypeIdentification(new ProjectIdentification(1), 2));
    }

    // AccommodationInviteIdentification — 2 листа: projectId, accommodationInviteId
    [Theory]
    [InlineData("AccommodationInviteId(1-2)")]
    [InlineData("AccommodationInvite(1-2)")]
    [InlineData("AccommodationInviteIdentification(1-2)")]
    [InlineData("1-2")]
    public void AccommodationInviteShouldParse(string val)
    {
        AccommodationInviteIdentification.TryParse(val, provider: null, out var result).ShouldBeTrue();
        result.ShouldBe(new AccommodationInviteIdentification(new ProjectIdentification(1), 2));
    }

    // AccommodationGroupIdentification — 2 листа: projectId, знаковое значение
    [Theory]
    [InlineData("AccommodationGroupId(1-2)")]
    [InlineData("AccommodationGroup(1-2)")]
    [InlineData("AccommodationGroupIdentification(1-2)")]
    [InlineData("1-2")]
    public void AccommodationGroupShouldParse(string val)
    {
        AccommodationGroupIdentification.TryParse(val, provider: null, out var result).ShouldBeTrue();
        result.ShouldBe(new AccommodationGroupIdentification(new ProjectIdentification(1), 2));
    }

    // Отрицательное значение (группа — заявка на проживание) обязано пережить round-trip
    [Fact]
    public void AccommodationGroupWithNegativeValueShouldRoundTrip()
    {
        var group = new AccommodationGroupIdentification(new ProjectIdentification(1), -42);
        var asString = group.ToString();
        asString.ShouldBe("AccommodationGroupId(1--42)");

        AccommodationGroupIdentification.TryParse(asString, provider: null, out var result).ShouldBeTrue();
        result.ShouldBe(group);
    }

    // Тип раньше назывался «целью приглашения»: строки со старым префиксом могли остаться в адресах
    // виджета приглашений, поэтому должны разбираться, а выдаваться — уже в новой форме.
    [Theory]
    [InlineData("AccommodationTargetId(1--42)", -42)]
    [InlineData("AccommodationTarget(1-2)", 2)]
    public void AccommodationGroupShouldParseLegacyPrefix(string val, int signedValue)
    {
        var expected = new AccommodationGroupIdentification(new ProjectIdentification(1), signedValue);

        AccommodationGroupIdentification.TryParse(val, provider: null, out var result).ShouldBeTrue();
        result.ShouldBe(expected);

        ProjectEntityIdParser.TryParseId(val, out var parsed).ShouldBeTrue();
        parsed.ShouldBe(expected);

        JsonSerializer.Deserialize<AccommodationGroupIdentification>(JsonSerializer.Serialize(val))
            .ShouldBe(expected);

        result.ToString().ShouldBe($"AccommodationGroupId(1-{signedValue})");
    }

    [Fact]
    public void AccommodationGroupFromClaimShouldConvertBackToClaim()
    {
        var claimId = new ClaimIdentification(new ProjectIdentification(1), 2);
        var group = AccommodationGroupIdentification.From(claimId);

        group.AsClaimId().ShouldBe(claimId);
        group.AsAccommodationRequestId().ShouldBeNull();
    }

    [Fact]
    public void AccommodationGroupFromRequestShouldConvertBackToRequest()
    {
        var requestId = new AccommodationRequestIdentification(new ProjectIdentification(1), 2);
        var group = AccommodationGroupIdentification.From(requestId);

        group.AsAccommodationRequestId().ShouldBe(requestId);
        group.AsClaimId().ShouldBeNull();
    }
}
