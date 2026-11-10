using Bunit;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Accommodation.Test;

/// <summary>
/// Форма приглашения валидирует выбранную цель через <see cref="EditContext"/>: пока селектор не
/// уведомил его об изменении поля, сообщение валидации не гаснет, а кнопка отправки остаётся
/// выключенной — для пользователя это «выбрал, кого пригласить, а форма всё равно ругается».
/// </summary>
public class AccommodationInviteTargetSelectorTest
{
    private static readonly ProjectIdentification ProjectId = new(123);
    private static readonly ClaimIdentification ClaimId = new(ProjectId, 5);
    private static readonly AccommodationRequestIdentification SenderRequestId = new(ProjectId, 7);

    private sealed class FormModel
    {
        public AccommodationGroupIdentification? Target { get; set; }
    }

    private static BunitContext CreateContext(FakeAccommodationInviteClient client)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IAccommodationInviteClient>(client);
        _ = ctx.Services.AddJoinRpgWebComponents();
        _ = ctx.AddBunitPersistentComponentState();
        // Интерактивный режим: выбор пункта уходит наружу через колбэки компонента.
        ctx.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        return ctx;
    }

    private static AccommodationInviteTargetViewModel Target(int claimId)
        => new(
            AccommodationGroupIdentification.From(new ClaimIdentification(ProjectId, claimId)),
            Text: $"Игрок {claimId}",
            ExtraSearch: $"Персонаж {claimId}",
            Subtext: "еще не выбрал тип проживания");

    /// <summary>
    /// В <c>EditForm</c> валидация перерисовывает сообщение об ошибке и состояние кнопки по событию
    /// <c>OnFieldChanged</c>. Без уведомления выбор цели не гасит ошибку — выглядит как «валидация
    /// не отпускает, хотя цель выбрана».
    /// </summary>
    [Fact]
    public void Selecting_NotifiesEditContextThatFieldChanged()
    {
        var firstTarget = Target(101);
        var secondTarget = Target(102);
        var client = new FakeAccommodationInviteClient
        {
            NextTargets = new AccommodationInviteTargetsViewModel(
                SenderRequestId, RoomFreeSpace: 2, Targets: [firstTarget, secondTarget], AccommodationInviteMode.Invite),
        };
        var ctx = CreateContext(client);
        using var _unused = ctx;
        var model = new FormModel();
        var editContext = new EditContext(model);
        var changedFields = new List<string>();
        editContext.OnFieldChanged += (_, args) => changedFields.Add(args.FieldIdentifier.FieldName);

        var cut = ctx.Render<AccommodationInviteTargetSelector>(p => p
            .AddCascadingValue(editContext)
            .Add(x => x.ClaimId, ClaimId)
            .Add(x => x.ValueExpression, () => model.Target)
            .Add(x => x.ValueChanged, v => model.Target = v));

        // Первый input — пустой пункт «(выберите, кого пригласить)».
        cut.FindAll(".join-select__list input")[2].Change(true);

        model.Target.ShouldBe(secondTarget.TargetId);
        changedFields.ShouldBe([nameof(FormModel.Target)]);
    }
}
