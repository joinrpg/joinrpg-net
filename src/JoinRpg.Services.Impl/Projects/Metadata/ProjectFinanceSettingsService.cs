using System.Data;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.Services.Impl.Projects.Metadata;

/// <summary>
/// Настройки финансов проекта — всё, что попадает в
/// <see cref="ProjectInfo.ProjectFinanceSettings"/>: типы оплаты, расписание взносов и общие
/// финансовые флаги. Операции по конкретным заявкам (приём взноса, переводы) живут отдельно,
/// в <see cref="FinanceOperationsImpl"/>.
/// </summary>
internal class ProjectFinanceSettingsService(
    IProjectPropsService projectPropsService,
    IVirtualUsersService vpu) : IProjectFinanceSettingsService
{
    /// <inheritdoc />
    public Task CreatePaymentType(CreatePaymentTypeRequest request)
        => projectPropsService.ChangeProjectProperties(
            new ProjectIdentification(request.ProjectId),
            Permission.CanManageMoney,
            ProjectActiveRequirement.MustBeActive,
            request,
            ctx =>
            {
                var paymentTypes = ctx.ProjectInfo.ProjectFinanceSettings.PaymentTypes;

                // Preparing master Id and checking if the same payment type already created
                int masterId;
                if (!ctx.Request.TypeKind.IsOnline())
                {
                    _ = ctx.ProjectInfo.RequestMasterAccess(ctx.Request.TargetMasterId);

                    // Cash payment could be only one
                    if (ctx.Request.TypeKind == PaymentTypeKind.Cash
                        && paymentTypes.Any(pt => pt.User.UserId == ctx.Request.TargetMasterId && pt.TypeKind == PaymentTypeKind.Cash))
                    {
                        throw new JoinRpgInvalidUserException($@"Payment of type ${ctx.Request.TypeKind.GetDisplayName()} is already created for the user ${ctx.Request.TargetMasterId}");
                    }

                    masterId = ctx.Request.TargetMasterId!.Value;
                }
                else
                {
                    if (paymentTypes.Any(pt => pt.TypeKind == ctx.Request.TypeKind))
                    {
                        throw new DataException($"Can't create more than one {ctx.Request.TypeKind} payment type");
                    }

                    masterId = vpu.PaymentsUser.UserId;
                }

                // Creating payment type
                var result = new PaymentType(ctx.Request.TypeKind, ctx.Request.ProjectId, masterId);

                // Configuring payment type
                if (result.TypeKind == PaymentTypeKind.Custom)
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace(ctx.Request.Name);
                    // Checking custom payment type name
                    result.Name = ctx.Request.Name.Trim();
                }

                ctx.Project.PaymentTypes.Add(result);
            });

    /// <inheritdoc />
    public Task TogglePaymentActiveness(ProjectIdentification projectId, int paymentTypeId)
        // Право проверяется внутри мутации: оно зависит от вида платежа и от того, включаем мы его
        // или выключаем (включить онлайн-оплату может только админ).
        => projectPropsService.ChangeProjectProperties(
            projectId,
            Permission.None,
            ProjectActiveRequirement.MustBeActive,
            paymentTypeId,
            ctx =>
            {
                var paymentType = ctx.GetPaymentTypeForChange(ctx.Request);

                switch (paymentType.TypeKind)
                {
                    case PaymentTypeKind.Custom:
                    case PaymentTypeKind.Cash:
                        ctx.RequireManageMoney();
                        break;
                    case PaymentTypeKind.Online:
                    case PaymentTypeKind.OnlineSubscription:
                        if (!ctx.CurrentUser.IsAdmin)
                        {
                            // Regular master with finance management permissions can disable online payments
                            if (paymentType.IsActive)
                            {
                                ctx.RequireManageMoney();
                            }
                            // ...but to enable them back he must have admin permissions
                            else
                            {
                                throw new MustBeAdminException();
                            }
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(paymentType.TypeKind), paymentType.TypeKind, null);
                }

                if (paymentType.IsActive)
                {
                    _ = ctx.SmartDelete(paymentType);
                }
                else
                {
                    paymentType.IsActive = true;
                }
            });

    /// <inheritdoc />
    public Task EditCustomPaymentType(ProjectIdentification projectId,
        int paymentTypeId,
        string name,
        bool isDefault)
        => projectPropsService.ChangeProjectProperties(
            projectId,
            Permission.CanManageMoney,
            ProjectActiveRequirement.MustBeActive,
            (paymentTypeId, name, isDefault),
            ctx =>
            {
                var paymentType = ctx.GetPaymentTypeForChange(ctx.Request.paymentTypeId);

                paymentType.IsActive = true;
                paymentType.Name = ServiceValidation.Required(ctx.Request.name);

                if (ctx.Request.isDefault && !paymentType.IsDefault)
                {
                    foreach (var oldDefault in ctx.Project.PaymentTypes.Where(pt => pt.IsDefault))
                    {
                        oldDefault.IsDefault = false;
                    }
                }

                paymentType.IsDefault = ctx.Request.isDefault;
            });

    /// <inheritdoc />
    public Task CreateFeeSetting(CreateFeeSettingRequest request)
        => projectPropsService.ChangeProjectProperties(
            new ProjectIdentification(request.ProjectId),
            Permission.CanManageMoney,
            ProjectActiveRequirement.MustBeActive,
            request,
            ctx =>
            {
                if (ctx.Request.StartDate < ctx.Now.UtcDateTime.Date.AddDays(-1))
                {
                    throw new CannotPerformOperationInPast();
                }

                if (!ctx.ProjectInfo.ProjectFinanceSettings.PreferentialFeeEnabled && ctx.Request.PreferentialFee != null)
                {
                    throw new PreferentialFeeNotEnabled();
                }

                ctx.Project.ProjectFeeSettings.Add(new ProjectFeeSetting()
                {
                    Fee = ctx.Request.Fee,
                    StartDate = ctx.Request.StartDate,
                    ProjectId = ctx.Request.ProjectId,
                    PreferentialFee = ctx.Request.PreferentialFee,
                });

                // Самая ранняя строка расписания всегда действует с момента создания проекта —
                // иначе до её начала взнос был бы не определён.
                var firstFee = ctx.Project.ProjectFeeSettings.OrderBy(s => s.StartDate).First();
                firstFee.StartDate = ctx.Project.CreatedDate;
            });

    /// <inheritdoc />
    public Task DeleteFeeSetting(ProjectIdentification projectId, int projectFeeSettingId)
        => projectPropsService.ChangeProjectProperties(
            projectId,
            Permission.CanManageMoney,
            ProjectActiveRequirement.MustBeActive,
            projectFeeSettingId,
            ctx =>
            {
                var feeSetting =
                    ctx.Project.ProjectFeeSettings.SingleOrDefault(pt =>
                        pt.ProjectFeeSettingId == ctx.Request)
                    ?? throw new JoinRpgEntityNotFoundException(ctx.Request, nameof(ProjectFeeSetting));

                if (feeSetting.StartDate < ctx.Now.UtcDateTime)
                {
                    throw new CannotPerformOperationInPast();
                }

                ctx.RemovePermanently(feeSetting);
            });

    /// <inheritdoc />
    public Task SaveGlobalSettings(SetFinanceSettingsRequest request)
        => projectPropsService.ChangeProjectProperties(
            new ProjectIdentification(request.ProjectId),
            Permission.CanManageMoney,
            ProjectActiveRequirement.MustBeActive,
            request,
            ctx =>
            {
                ctx.Project.Details.FinanceWarnOnOverPayment = ctx.Request.WarnOnOverPayment;
                ctx.Project.Details.PreferentialFeeEnabled = ctx.Request.PreferentialFeeEnabled;
                ctx.Project.Details.PreferentialFeeConditions =
                    new MarkdownDbValue(ctx.Request.PreferentialFeeConditions);
            });
}
