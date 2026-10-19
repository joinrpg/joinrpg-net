using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Finances;

namespace JoinRpg.Domain.Test;

/// <summary>
/// Расчёт взноса поверх доменного агрегата (ADR013). Прежний расчёт поверх EF-сущности
/// <see cref="Claim"/> удалён (#5419); ожидаемые суммы — те, что он давал на тех же данных.
/// </summary>
public class ClaimBalanceOverCharacterInfoTest
{
    private static readonly DateTime FeeStart = new(2026, 3, 1);
    private static readonly DateTime OperationDate = new(2026, 3, 15);

    private readonly MockedProject mock = new();
    private readonly ProjectFieldInfo pricedField;

    public ClaimBalanceOverCharacterInfoTest()
    {
        mock.Project.ProjectFeeSettings.Add(
            new ProjectFeeSetting { StartDate = FeeStart, Fee = 1000, PreferentialFee = 400 });

        pricedField = mock.AddField(field =>
        {
            field.FieldName = "Платное поле";
            field.FieldType = ProjectFieldType.Checkbox;
            field.Price = 250;
            field.FieldBoundTo = FieldBoundTo.Character;
        });
    }

    [Theory]
    // (зафиксированный взнос, льготник, включено ли платное поле, итоговый взнос)
    [InlineData(null, false, false, 1000)]
    [InlineData(null, false, true, 1000 + 250)]
    [InlineData(null, true, false, 400)]
    [InlineData(null, true, true, 400 + 250)]
    [InlineData(777, false, false, 777)]
    [InlineData(777, true, true, 777 + 250)]
    public void BalanceSumsBaseFeeAndFields(int? currentFee, bool preferential, bool fieldSet, int expectedTotal)
    {
        var fieldsJson = fieldSet ? $$"""{"{{pricedField.Id.ProjectFieldId}}":"on"}""" : null;

        var (character, claim) = MakeAggregate(currentFee, preferential, fieldsJson);
        var actual = new ClaimInCharacter(character, claim).CalculateBalance(OperationDate);

        actual.ShouldBe(new ClaimBalance(FeePaid: 0, TotalFee: expectedTotal));
    }

    [Fact]
    public void PricedFieldIsActuallyCounted()
    {
        // Взнос за поле — разница между заявками, а не абсолютная сумма из расписания.
        var fieldsJson = $$"""{"{{pricedField.Id.ProjectFieldId}}":"on"}""";

        var (withField, claimWithField) = MakeAggregate(currentFee: 1000, preferential: false, fieldsJson);
        var (without, claimWithout) = MakeAggregate(currentFee: 1000, preferential: false, fieldsJson: null);

        new ClaimInCharacter(withField, claimWithField).CalculateBalance(OperationDate).TotalFee
            .ShouldBe(new ClaimInCharacter(without, claimWithout).CalculateBalance(OperationDate).TotalFee + 250);
    }

    [Fact]
    public void FeePaidAndAccommodationAreAddedToBalance()
    {
        // Проживание и оплаченное — простые слагаемые, в агрегате они лежат готовыми числами.
        var (character, claim) = MakeAggregate(
            currentFee: 1000, preferential: false, fieldsJson: null, feePaid: 300, accommodationFee: 150);

        var balance = new ClaimInCharacter(character, claim).CalculateBalance(OperationDate);

        balance.FeePaid.ShouldBe(300);
        balance.TotalFee.ShouldBe(1000 + 150);
        balance.FeeDue.ShouldBe(1000 + 150 - 300);
    }

    [Fact]
    public void FeeBreakdownSplitsFieldsByBoundAndAddsUpToBalance()
    {
        var claimField = mock.AddField(field =>
        {
            field.FieldName = "Платное поле заявки";
            field.FieldType = ProjectFieldType.Checkbox;
            field.Price = 70;
            field.FieldBoundTo = FieldBoundTo.Claim;
        });
        // Платное, но не отмеченное: в сумму не входит, но платным полем считается.
        mock.AddField(field =>
        {
            field.FieldName = "Неотмеченное платное поле заявки";
            field.FieldType = ProjectFieldType.Checkbox;
            field.Price = 30;
            field.FieldBoundTo = FieldBoundTo.Claim;
        });

        var character = mock.CreateCharacter("С платными полями");
        character.JsonData = $$"""{"{{pricedField.Id.ProjectFieldId}}":"on"}""";
        var claim = mock.CreateApprovedClaim(character, mock.Player);
        claim.CurrentFee = 1000;
        claim.JsonData = $$"""{"{{claimField.Id.ProjectFieldId}}":"on"}""";
        claim.FinanceOperations = [];

        var claimInCharacter = new ClaimInCharacter(mock.GetCharacterInfo(character), claim.GetId());
        var breakdown = claimInCharacter.CalculateFeeBreakdown(OperationDate);

        breakdown.BaseFee.ShouldBe(1000);
        breakdown.IsBaseFeeFixed.ShouldBeTrue();
        breakdown.BaseFeeSetting.ShouldBeNull();
        breakdown.HasBaseFee.ShouldBeTrue();
        breakdown.CharacterFields.ShouldBe(new FieldsFeeSubtotal(Fee: 250, FieldsWithFeeCount: 1));
        breakdown.ClaimFields.ShouldBe(new FieldsFeeSubtotal(Fee: 70, FieldsWithFeeCount: 2));
        breakdown.HasFieldsWithFee.ShouldBeTrue();
        breakdown.TotalFee.ShouldBe(1000 + 250 + 70);
        breakdown.TotalFee.ShouldBe(claimInCharacter.CalculateBalance(OperationDate).TotalFee);
    }

    [Fact]
    public void FeeBreakdownTakesBaseFeeFromScheduleAndAccommodationFromSnapshot()
    {
        var (character, claim) = MakeAggregate(
            currentFee: null, preferential: true, fieldsJson: null, accommodationFee: 150);

        var breakdown = new ClaimInCharacter(character, claim).CalculateFeeBreakdown(OperationDate);

        breakdown.BaseFee.ShouldBe(400);
        breakdown.IsBaseFeeFixed.ShouldBeFalse();
        breakdown.BaseFeeSetting.ShouldNotBeNull().StartDate.ShouldBe(FeeStart);
        breakdown.HasBaseFee.ShouldBeTrue();
        breakdown.AccommodationFee.ShouldBe(150);
        breakdown.HasFieldsWithFee.ShouldBeTrue(); // платное поле проекта есть, хоть и не отмечено
        breakdown.FieldsFee.ShouldBe(0);
        breakdown.TotalFee.ShouldBe(400 + 150);
    }

    [Fact]
    public void BeforeFeeScheduleStartsBaseFeeIsZero()
    {
        var (character, claim) = MakeAggregate(currentFee: null, preferential: false, fieldsJson: null);

        var claimInCharacter = new ClaimInCharacter(character, claim);
        var balance = claimInCharacter.CalculateBalance(FeeStart.AddDays(-1));

        balance.TotalFee.ShouldBe(0);
        // Строки «Взнос» на странице заявки тогда нет вовсе.
        claimInCharacter.CalculateFeeBreakdown(FeeStart.AddDays(-1)).HasBaseFee.ShouldBeFalse();
    }

    /// <summary>
    /// Неутверждённая заявка не платит за непубличные поля персонажа: её игрок их не видит
    /// (<c>CharacterFieldLayers.ForUnapprovedClaim</c>). Утверждённая — платит. Так считала
    /// EF-версия, а доменная до исправления брала все поля персонажа.
    /// </summary>
    [Theory]
    [InlineData(false, 1000)]
    [InlineData(true, 1000 + 300)]
    public void HiddenCharacterFieldIsPaidOnlyByApprovedClaim(bool approved, int expectedTotal)
    {
        var hiddenField = mock.AddField(field =>
        {
            field.FieldName = "Скрытое платное поле";
            field.FieldType = ProjectFieldType.Checkbox;
            field.Price = 300;
            field.FieldBoundTo = FieldBoundTo.Character;
            field.IsPublic = false;
            field.CanPlayerView = false;
        });

        var character = mock.CreateCharacter("Со скрытым полем");
        character.JsonData = $$"""{"{{hiddenField.Id.ProjectFieldId}}":"on"}""";
        var claim = approved
            ? mock.CreateApprovedClaim(character, mock.Player)
            : mock.CreateClaim(character, mock.Player);
        claim.FinanceOperations = [];

        var actual = new ClaimInCharacter(mock.GetCharacterInfo(character), claim.GetId()).CalculateBalance(OperationDate);

        actual.TotalFee.ShouldBe(expectedTotal);
    }

    private (CharacterInfo Character, CharacterClaimInfo Claim) MakeAggregate(
        int? currentFee,
        bool preferential,
        string? fieldsJson,
        int feePaid = 0,
        int accommodationFee = 0)
    {
        var projectInfo = mock.ProjectInfo;
        var characterId = new CharacterIdentification(projectInfo.ProjectId, 100);
        var claimId = new ClaimIdentification(projectInfo.ProjectId, 200);

        var claim = new CharacterClaimInfo(
            claimId,
            mock.Player.ToUserInfoHeader(),
            ClaimStatus.Approved,
            DenialStatus: null,
            ResponsibleMasterId: new UserIdentification(mock.Master.UserId),
            CreateDate: OperationDate,
            LastUpdateDateTime: OperationDate,
            MasterAcceptedDate: null,
            MasterDeclinedDate: null,
            PlayerDeclinedDate: null,
            CheckInDate: null,
            LastPlayerCommentAt: null,
            LastMasterCommentAt: null,
            LastVisibleMasterCommentAt: null,
            CommentDiscussionId: new CommentDiscussionId(300),
            Finance: new ClaimFinanceInfo(
                FixedFee: currentFee,
                PreferentialFeeUser: preferential,
                FeePaid: feePaid,
                AccommodationFee: accommodationFee,
                OperationsRequireModeration: false),
            AccommodationTypeId: null,
            AccommodationGroupId: AccommodationGroupIdentification.From(claimId),
            PlayerAllowedSensitiveData: false,
            Fields: FieldLayerContainer.DeserializeFieldLayer(projectInfo, fieldsJson));

        var character = new CharacterInfo(
            characterId,
            projectInfo,
            "Агрегат",
            CharacterTypeInfo.Default(),
            hidePlayerForCharacter: false,
            isActive: true,
            inGame: false,
            autoCreated: false,
            new MarkdownString(""),
            originalCharacterSlotId: null,
            [projectInfo.GroupTree.RootGroupId],
            FieldLayerContainer.DeserializeFieldLayer(projectInfo, null),
            plotElementOrderData: null,
            [claim],
            claimId,
            OperationDate,
            new UserIdentification(mock.Master.UserId),
            OperationDate,
            new UserIdentification(mock.Master.UserId));

        return (character, claim);
    }
}
