using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Common.WebComponents;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.ProjectCommon.Test;

public class MasterSelectorTest
{
    private static readonly UserInfoHeader FirstMaster =
        new(new UserIdentification(1), new UserDisplayName("Мастер1", null), null);

    private static readonly UserInfoHeader SecondMaster =
        new(new UserIdentification(2), new UserDisplayName("Мастер2", null), null);

    private sealed class FormModel
    {
        public UserIdentification? MasterId { get; set; }
    }

    private sealed class FakeMasterClient : IMasterClient
    {
        public Task<List<UserInfoHeader>> GetMasters(int projectId)
            => Task.FromResult(new List<UserInfoHeader> { FirstMaster, SecondMaster });
    }

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IMasterClient>(new FakeMasterClient());
        _ = ctx.Services.AddJoinRpgWebComponents();
        _ = ctx.AddBunitPersistentComponentState();
        // Интерактивный режим: выбор пункта уходит наружу через колбэки компонента.
        ctx.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        return ctx;
    }

    /// <summary>
    /// В <c>EditForm</c> валидация перерисовывает сообщение об ошибке и состояние кнопки по событию
    /// <c>OnFieldChanged</c>. Без уведомления выбор мастера не гасит ошибку — выглядит как «валидация
    /// не отпускает, хотя мастера выбрал».
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

        var cut = ctx.Render<MasterSelector>(p => p
            .AddCascadingValue(editContext)
            .Add(x => x.ProjectId, 1)
            .Add(x => x.MasterIdExpression, () => model.MasterId)
            .Add(x => x.MasterIdChanged, id => model.MasterId = id));

        cut.FindAll(".join-select__list input")[1].Change(true);

        model.MasterId.ShouldBe(SecondMaster.UserId);
        changedFields.ShouldBe([nameof(FormModel.MasterId)]);
    }
}
