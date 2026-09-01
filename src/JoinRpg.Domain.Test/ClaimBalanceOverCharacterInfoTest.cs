using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test;

/// <summary>
/// Расчёт взноса поверх доменного агрегата (ADR013) должен совпадать с расчётом поверх EF-сущности
/// <see cref="Claim"/>. Тесты сравнивают две реализации на одних и тех же данных: пока старая жива,
/// это единственное место, где расхождение будет видно.
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
    // (зафиксированный взнос, льготник, включено ли платное поле)
    [InlineData(null, false, false)]
    [InlineData(null, false, true)]
    [InlineData(null, true, false)]
    [InlineData(null, true, true)]
    [InlineData(777, false, false)]
    [InlineData(777, true, true)]
    public void BalanceShouldMatchLegacyCalculation(int? currentFee, bool preferential, bool fieldSet)
    {
        var fieldsJson = fieldSet ? $$"""{"{{pricedField.Id.ProjectFieldId}}":"on"}""" : null;

        var legacy = MakeLegacyClaim(currentFee, preferential, fieldsJson)
            .CalculateClaimBalance(mock.ProjectInfo, OperationDate);

        var (character, claim) = MakeAggregate(currentFee, preferential, fieldsJson);
        var actual = character.CalculateClaimBalance(claim, mock.ProjectInfo, OperationDate);

        actual.ShouldBe(legacy);
    }

    [Fact]
    public void PricedFieldIsActuallyCounted()
    {
        // Страж от вырожденной проверки выше: если бы взнос за поле всегда считался нулём,
        // BalanceShouldMatchLegacyCalculation прошёл бы, ничего не проверив.
        var fieldsJson = $$"""{"{{pricedField.Id.ProjectFieldId}}":"on"}""";

        var (withField, claimWithField) = MakeAggregate(currentFee: 1000, preferential: false, fieldsJson);
        var (without, claimWithout) = MakeAggregate(currentFee: 1000, preferential: false, fieldsJson: null);

        withField.CalculateClaimBalance(claimWithField, mock.ProjectInfo, OperationDate).TotalFee
            .ShouldBe(without.CalculateClaimBalance(claimWithout, mock.ProjectInfo, OperationDate).TotalFee + 250);
    }

    [Fact]
    public void FeePaidAndAccommodationAreAddedToBalance()
    {
        // Проживание и оплаченное — простые слагаемые, в агрегате они лежат готовыми числами.
        var (character, claim) = MakeAggregate(
            currentFee: 1000, preferential: false, fieldsJson: null, feePaid: 300, accommodationFee: 150);

        var balance = character.CalculateClaimBalance(claim, mock.ProjectInfo, OperationDate);

        balance.FeePaid.ShouldBe(300);
        balance.TotalFee.ShouldBe(1000 + 150);
        balance.FeeDue.ShouldBe(1000 + 150 - 300);
    }

    [Fact]
    public void BeforeFeeScheduleStartsBaseFeeIsZero()
    {
        var (character, claim) = MakeAggregate(currentFee: null, preferential: false, fieldsJson: null);

        var balance = character.CalculateClaimBalance(claim, mock.ProjectInfo, FeeStart.AddDays(-1));

        balance.TotalFee.ShouldBe(0);
    }

    private Claim MakeLegacyClaim(int? currentFee, bool preferential, string? fieldsJson)
    {
        var character = mock.CreateCharacter("Легаси");
        var claim = mock.CreateApprovedClaim(character, mock.Player);
        claim.CurrentFee = currentFee;
        claim.PreferentialFeeUser = preferential;
        claim.FinanceOperations = [];
        claim.JsonData = fieldsJson;
        return claim;
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
            CheckInDate: null,
            LastPlayerCommentAt: null,
            LastMasterCommentAt: null,
            LastVisibleMasterCommentAt: null,
            CurrentFee: currentFee,
            PreferentialFeeUser: preferential,
            FeePaid: feePaid,
            AccommodationFee: accommodationFee,
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
            [projectInfo.RootCharacterGroupId],
            FieldLayerContainer.DeserializeFieldLayer(projectInfo, null),
            [claim],
            claimId,
            OperationDate,
            new UserIdentification(mock.Master.UserId),
            OperationDate,
            new UserIdentification(mock.Master.UserId));

        return (character, claim);
    }
}
