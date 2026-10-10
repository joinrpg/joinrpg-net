using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Impl.Accommodation;
using JoinRpg.Services.Impl.Characters;
using JoinRpg.Services.Impl.Claims;
using JoinRpg.Services.Interfaces.ProjectMetadata;

namespace JoinRpg.Services.Impl.Projects.Metadata;

/// <summary>
/// Типы проживания проекта — настройка мастера, часть <see cref="ProjectInfo"/> (ADR015), поэтому
/// меняются через <see cref="IProjectPropsService"/> (ADR009): права, активность проекта,
/// логирование и согласованность <c>Project</c>/<c>ProjectInfo</c> достаются оттуда.
/// </summary>
/// <remarks>
/// Группы проживания (<c>AccommodationRequest</c>) ссылаются на тип, но принадлежат агрегату
/// персонажа (ADR014, ADR018 §4), поэтому при удалении типа они расформировываются через
/// <see cref="ICharacterPropsService"/>, а не правкой <c>DbSet</c> отсюда.
/// </remarks>
internal class AccommodationTypeService(
    IProjectPropsService projectPropsService,
    ICharacterPropsService characterPropsService,
    IAccommodationRepository accommodationRepository) : IAccommodationTypeService
{
    /// <inheritdoc />
    public async Task<AccommodationTypeIdentification> CreateAccommodationType(
        ProjectIdentification projectId,
        AccommodationTypeRequest request,
        RoomCategorySelection? roomCategory = null)
    {
        var entity = await projectPropsService.ChangeProjectProperties(
            projectId,
            Permission.CanManageAccommodation,
            ProjectActiveRequirement.MustBeActive,
            (Request: request, RoomCategory: roomCategory ?? new NewRoomCategory(request.Name)),
            ctx =>
            {
                var category = ctx.Request.RoomCategory switch
                {
                    ExistingRoomCategory existing => GetRoomCategoryForChange(ctx, existing.Id),
                    NewRoomCategory created => CreateRoomCategory(ctx, created.Name),
                    _ => throw new ArgumentOutOfRangeException(nameof(roomCategory)),
                };

                var entity = new ProjectAccommodationType
                {
                    ProjectId = ctx.Project.ProjectId,
                    Project = ctx.Project,
                    RoomCategory = category,
                    // У новой категории Id ещё нет (0) — его проставит EF при сохранении по навигации.
                    RoomCategoryId = category.Id,
                    Desirous = [],
                };
                // Коллекцию типов категории не трогаем: у существующей категории она не загружена,
                // и обращение к ней — ленивая загрузка. Связь держат навигация и RoomCategoryId.
                Apply(
                    entity,
                    ctx.Request.Request.Name,
                    ctx.Request.Request.Description,
                    ctx.Request.Request.Cost,
                    ctx.Request.Request.Capacity,
                    ctx.Request.Request.IsPlayerSelectable);
                ctx.Project.ProjectAccommodationTypes.Add(entity);
                return entity;
            });

        // Id генерируется базой при SaveChanges — читаем уже после возврата из props-сервиса.
        return entity.GetId();
    }

    /// <inheritdoc />
    public Task RenameRoomCategory(RoomCategoryIdentification roomCategoryId, string name)
        => projectPropsService.ChangeProjectProperties(
            roomCategoryId.ProjectId,
            Permission.CanManageAccommodation,
            ProjectActiveRequirement.MustBeActive,
            (RoomCategoryId: roomCategoryId, Name: name),
            ctx =>
            {
                var category = GetRoomCategoryForChange(ctx, ctx.Request.RoomCategoryId);
                category.Name = ServiceValidation.Required(ctx.Request.Name);
            });

    private static ProjectRoomCategory CreateRoomCategory(ProjectMutationContext ctx, string name)
    {
        var category = new ProjectRoomCategory
        {
            ProjectId = ctx.Project.ProjectId,
            Project = ctx.Project,
            Name = ServiceValidation.Required(name),
            Rooms = [],
            AccommodationTypes = [],
        };
        ctx.Project.ProjectRoomCategories.Add(category);
        return category;
    }

    private static ProjectRoomCategory GetRoomCategoryForChange(ProjectMutationContext ctx, RoomCategoryIdentification id)
    {
        // Категория чужого проекта сюда не попадёт: ищем только среди категорий этого проекта.
        if (id.ProjectId != ctx.ProjectInfo.ProjectId)
        {
            throw new RoomCategoryNotFoundException(id);
        }
        return ctx.Project.ProjectRoomCategories.SingleOrDefault(c => c.Id == id.RoomCategoryId)
            ?? throw new RoomCategoryNotFoundException(id);
    }

    /// <inheritdoc />
    public Task UpdateAccommodationType(
        AccommodationTypeIdentification accommodationTypeId,
        AccommodationTypeRequest request)
        => projectPropsService.ChangeProjectProperties(
            accommodationTypeId.ProjectId,
            Permission.CanManageAccommodation,
            ProjectActiveRequirement.MustBeActive,
            (AccommodationTypeId: accommodationTypeId, Request: request),
            ctx =>
            {
                var entity = ctx.GetAccommodationTypeForChange(ctx.Request.AccommodationTypeId);

                // Категория, созданная вместе с типом, носит его имя (ADR020): пока имена совпадают,
                // переименование типа переименовывает и её — иначе письма о заселении (они
                // называют комнату по категории) остались бы со старым именем. Имя, которое
                // мастер дал категории отдельно, не трогаем, и общую с другими типами — тоже.
                var category = ctx.Project.ProjectRoomCategories.Single(c => c.Id == entity.RoomCategoryId);
                var isOnlyTypeOfCategory = !ctx.Project.ProjectAccommodationTypes
                    .Any(type => type.RoomCategoryId == entity.RoomCategoryId && type.Id != entity.Id);
                if (isOnlyTypeOfCategory && category.Name == entity.Name)
                {
                    category.Name = ServiceValidation.Required(ctx.Request.Request.Name);
                }

                Apply(
                    entity,
                    ctx.Request.Request.Name,
                    ctx.Request.Request.Description,
                    ctx.Request.Request.Cost,
                    ctx.Request.Request.Capacity,
                    ctx.Request.Request.IsPlayerSelectable);
            });

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Нерасселённые группы этого типа расформировываются: каждая заявка выходит из своей группы,
    /// как если бы у неё сбросили тип проживания, и опустевшая группа удаляется. Раньше этого не
    /// было, и база, удаляя тип, каскадом удаляла его группы, на которые ещё ссылались заявки, —
    /// удаление падало на внешнем ключе <c>Claims.AccommodationRequest_Id</c>.
    /// </para>
    /// <para>
    /// Атомарности нет: каждая заявка — отдельная мутация своего агрегата со своим сохранением, а
    /// сам тип удаляется последней операцией над настройками проекта. Зато всё идемпотентно: если
    /// удаление оборвалось на середине, тип остался, и повторное удаление дочистит оставшиеся
    /// группы. Порядок выбран так, чтобы оборванное удаление никогда не оставляло заявку со
    /// ссылкой на несуществующий тип.
    /// </para>
    /// </remarks>
    public async Task DeleteAccommodationType(AccommodationTypeIdentification accommodationTypeId)
    {
        // Комнаты и их жильцы — оперативные данные, в граф метаданных проекта они не входят
        // (ADR015), поэтому занятость проверяем отдельным запросом, а не по ctx.Project: иначе
        // получилась бы ленивая догрузка, которую операциям props-сервиса делать нельзя (#4987).
        var hasOccupiedRoom = await accommodationRepository.HasOccupiedRoomOfType(accommodationTypeId);

        // Занятый тип удалять нельзя — тогда и группы не трогаем: отказ ниже, в props-сервисе,
        // после проверки прав, как и раньше.
        if (!hasOccupiedRoom)
        {
            foreach (var claimId in await accommodationRepository.GetClaimsInGroupsOfType(accommodationTypeId))
            {
                await LeaveGroupOfDeletedType(claimId, accommodationTypeId);
            }
        }

        await projectPropsService.ChangeProjectProperties(
            accommodationTypeId.ProjectId,
            Permission.CanManageAccommodation,
            ProjectActiveRequirement.MustBeActive,
            accommodationTypeId,
            ctx =>
            {
                var entity = ctx.GetAccommodationTypeForChange(ctx.Request);

                if (hasOccupiedRoom)
                {
                    throw new AccommodationTypeIsOccupiedException(ctx.Request);
                }

                // Последний тип категории уносит её с собой, а её комнаты удаляет каскад БД —
                // так же, как раньше удаление типа уносило его комнаты (ADR020, §4). Если у
                // категории остались сестринские типы, комнаты остаются им.
                var categoryId = entity.RoomCategoryId;
                var isLastOfCategory = !ctx.Project.ProjectAccommodationTypes
                    .Any(type => type.RoomCategoryId == categoryId && type.Id != entity.Id);
                var category = ctx.Project.ProjectRoomCategories.Single(c => c.Id == categoryId);

                ctx.RemovePermanently(entity);

                if (isLastOfCategory)
                {
                    ctx.RemovePermanently(category);
                }
            });
    }

    /// <summary>
    /// Выводит заявку из её группы проживания удаляемого типа — мутация агрегата этой заявки.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ядро общее с операциями <c>ClaimServiceImpl</c> — <see cref="ClaimServiceImpl.ConsiderLeavingRoom"/>:
    /// заявка убирается из состава группы, опустевшая группа удаляется. Новой одноместной группы,
    /// в отличие от <c>LeaveAccommodationGroupAsync</c>, не заводится — её типа больше нет.
    /// </para>
    /// <para>
    /// Права — те же, что у удаления типа (<see cref="ClaimAccessRequirement.ManageAccommodation"/>), а
    /// не право расселять: расформирование — следствие удаления настройки, а не отдельное действие.
    /// </para>
    /// </remarks>
    private Task LeaveGroupOfDeletedType(
        ClaimIdentification claimId,
        AccommodationTypeIdentification accommodationTypeId)
        => characterPropsService.ChangeClaimAsync(
            claimId,
            ClaimAccessRequirement.ManageAccommodation,
            ProjectActiveRequirement.MustBeActive,
            accommodationTypeId,
            async ctx =>
            {
                // Заявку успели вывести из группы или перевести в другой тип между запросом
                // списка и этой мутацией — делать нечего. Решается по снимку заявки (ADR022 §4),
                // план тогда даже не грузится.
                if (ctx.CharacterClaimInfo.AccommodationTypeId != ctx.Request)
                {
                    ctx.NothingChanged();
                    return;
                }

                var ownGroup = await ctx.LoadOwnAccommodationGroup();

                // Группу успели расселить после проверки занятости — удалять тип уже нельзя.
                if (ownGroup?.Group.RoomId is not null)
                {
                    throw new AccommodationTypeIsOccupiedException(ctx.Request);
                }

                // Группа не расселена, значит уведомления о выезде из комнаты не будет. Ссылку
                // заявки на группу обнуляет сам ConsiderLeavingRoom, новой группы не заводим.
                _ = ClaimServiceImpl.ConsiderLeavingRoom(ctx, ownGroup, RoomOccupancyChangeKind.LeftRoom);

                await DeclineUnansweredInvites(ctx);
            });

    /// <summary>
    /// Отклоняет неотвеченные приглашения к совместному проживанию, в которых участвует заявка:
    /// группы, куда звали или откуда звали, больше нет, и висеть им незачем.
    /// </summary>
    /// <remarks>
    /// Уже отвеченные приглашения — история, их не трогаем. Остальным участникам уходит
    /// уведомление о снятии приглашения, как и при отказе от заявки.
    /// </remarks>
    private static async Task DeclineUnansweredInvites(ClaimMutationContext ctx)
    {
        var recipients = new List<ClaimIdentification>();

        foreach (var invite in await ctx.LoadInvitesForClaim())
        {
            if (invite.IsAccepted != InviteState.Unanswered)
            {
                continue;
            }

            invite.IsAccepted = InviteState.Declined;
            invite.ResolveDescription = ResolveDescription.DeclinedAuto;

            var counterpartyId = invite.FromClaimId == ctx.Claim.ClaimId ? invite.ToClaimId : invite.FromClaimId;
            var counterparty = new ClaimIdentification(ctx.ProjectInfo.ProjectId, counterpartyId);
            if (!recipients.Contains(counterparty))
            {
                recipients.Add(counterparty);
            }
        }

        if (recipients.Count > 0)
        {
            ctx.AddInviteNotification(new AccommodationInviteNotification(
                recipients,
                ctx.CurrentUser.ToUserInfoHeader(),
                InviteChangeKind.Cancelled));
        }
    }

    private static void Apply(
        ProjectAccommodationType entity,
        string name,
        MarkdownString description,
        int cost,
        int capacity,
        bool isPlayerSelectable)
    {
        entity.Name = ServiceValidation.Required(name);
        entity.Description = new MarkdownDbValue(description.Value);
        entity.Cost = cost;
        entity.Capacity = capacity;
        entity.IsPlayerSelectable = isPlayerSelectable;
    }
}
