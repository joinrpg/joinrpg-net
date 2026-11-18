using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.WebComponents;
using JoinRpg.Web.ProjectCommon.KogdaIgra;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.ProjectCommon.Test;

/// <summary>
/// Форма создания проекта валидирует выбранную игру через <see cref="EditContext"/>: пока селектор
/// не уведомил его об изменении поля, кнопка создания остаётся выключенной, а сообщение валидации
/// не гаснет — для пользователя это «выбрал игру, а форма всё равно ругается».
/// </summary>
public class KogdaIgraGameSelectorTest
{
    private static readonly KogdaIgraShortViewModel FirstGame =
        new(new KogdaIgraIdentification(101), "Первая игра", new Uri("https://kogda-igra.ru/game/101"), Year: 2026);

    private static readonly KogdaIgraShortViewModel SecondGame =
        new(new KogdaIgraIdentification(102), "Вторая игра", new Uri("https://kogda-igra.ru/game/102"), Year: 2026);

    private sealed class FormModel
    {
        public KogdaIgraIdentification? KogdaIgraGameId { get; set; }
    }

    private sealed class FakeKogdaIgraSyncClient : IKogdaIgraSyncClient
    {
        public Task<ResyncOperationResultsViewModel> ResyncKograIgra() => throw new NotImplementedException();
        public Task<SyncStatusViewModel> GetSyncStatus() => throw new NotImplementedException();
        public Task<KogdaIgraShortViewModel[]> GetKogdaIgraCandidates() => throw new NotImplementedException();
        public Task<KogdaIgraShortViewModel[]> GetFutureKogdaIgraCandidates() => throw new NotImplementedException();
        public Task<KogdaIgraShortViewModel[]> GetKogdaIgraNotUpdated() => throw new NotImplementedException();
        public Task<KogdaIgraCardViewModel[]> GetKogdaIgraCards(IReadOnlyCollection<KogdaIgraIdentification> kogdaIgraIds) => throw new NotImplementedException();
        public Task<ResyncOperationResultsViewModel> ForceResyncGames(KogdaIgraIdentification[] gameIds) => throw new NotImplementedException();
        public Task<ResyncOperationResultsViewModel> ScheduleUpdateAllGames() => throw new NotImplementedException();
        public Task<ResyncOperationResultsViewModel> RunSyncJob() => throw new NotImplementedException();
    }

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IKogdaIgraSyncClient>(new FakeKogdaIgraSyncClient());
        _ = ctx.Services.AddJoinRpgWebComponents();
        _ = ctx.AddBunitPersistentComponentState();
        // Интерактивный режим: выбор пункта уходит наружу через колбэки компонента.
        ctx.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        return ctx;
    }

    /// <summary>
    /// В <c>EditForm</c> валидация перерисовывает сообщение об ошибке и состояние кнопки по событию
    /// <c>OnFieldChanged</c>. Без уведомления выбор игры не гасит ошибку — выглядит как «валидация
    /// не отпускает, хотя игру выбрал».
    /// </summary>
    [Fact]
    public void Selecting_NotifiesEditContextThatFieldChanged()
    {
        var ctx = CreateContext();
        using var _unused = ctx;
        var model = new FormModel();
        var editContext = new EditContext(model);
        var changedFields = new List<string>();
        editContext.OnFieldChanged += (_, args) => changedFields.Add(args.FieldIdentifier.FieldName);

        var cut = ctx.Render<KogdaIgraGameSelector>(p => p
            .AddCascadingValue(editContext)
            .Add(x => x.Multiple, false)
            .Add(x => x.InitialList, [FirstGame, SecondGame])
            .Add(x => x.KogdaIgraGameIdExpression, () => model.KogdaIgraGameId)
            .Add(x => x.KogdaIgraIdsChanged, ids => model.KogdaIgraGameId = ids.FirstOrDefault()));

        cut.FindAll(".join-select__list input")[1].Change(true);

        model.KogdaIgraGameId.ShouldBe(SecondGame.KogdaIgraId);
        changedFields.ShouldBe([nameof(FormModel.KogdaIgraGameId)]);
    }
}
