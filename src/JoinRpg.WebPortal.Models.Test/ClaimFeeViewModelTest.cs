using JoinRpg.DataModel;
using JoinRpg.DataModel.Extensions;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.WebPortal.Models.Test;

/// <summary>
/// Когда в заявке виден блок взноса — а с ним и кнопки «Отметить оплату…» и «Изменить взнос».
/// </summary>
public class ClaimFeeViewModelTest
{
    private readonly MockedProject mock = new();

    private ClaimFeeViewModel Build(Claim claim)
    {
        var claimInCharacter = new ClaimInCharacter(mock.GetCharacterInfo(claim.Character), claim.GetId());
        return new ClaimFeeViewModel(
            claim,
            claimInCharacter,
            new UserIdentification(mock.Master.UserId),
            _ => null,
            accommodation: null);
    }

    [Fact]
    public void NoFee_PaidAccommodationNotSelected_FeeShown()
    {
        // #5646: взноса в проекте нет, поселение платное, а проживание в заявке не выбрано —
        // мастер всё равно должен иметь возможность отметить оплату.
        mock.Project.Details.EnableAccommodation = true;
        _ = mock.CreateAccommodationType("Домик", cost: 1000);
        mock.ReInitProjectInfo();
        var claim = mock.CreateApprovedClaim(mock.CreateCharacter("Бездомный"), mock.Player);

        var model = Build(claim);

        model.AccommodationFee.ShouldBe(0);
        model.ShowFee.ShouldBeTrue();
    }

    [Fact]
    public void NoFee_AccommodationDisabled_FeeHidden()
    {
        _ = mock.CreateAccommodationType("Домик", cost: 1000);
        mock.ReInitProjectInfo();
        var claim = mock.CreateApprovedClaim(mock.CreateCharacter("Бесплатный"), mock.Player);

        Build(claim).ShowFee.ShouldBeFalse();
    }

    [Fact]
    public void NoFee_OnlyFreeAccommodation_FeeHidden()
    {
        mock.Project.Details.EnableAccommodation = true;
        _ = mock.CreateAccommodationType("Своя палатка", cost: 0);
        mock.ReInitProjectInfo();
        var claim = mock.CreateApprovedClaim(mock.CreateCharacter("Турист"), mock.Player);

        Build(claim).ShowFee.ShouldBeFalse();
    }
}
