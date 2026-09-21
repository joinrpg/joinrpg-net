using JoinRpg.Interfaces;
using JoinRpg.Web.ProjectCommon;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Services.Advertisement;

[JobDelay(9)] // 09:00 UTC = 12:00 МСК
internal class AdvertisementJob(
    IAdvertisementScheduleRepository scheduleRepository,
    IAdvertisementLogRepository logRepository,
    IProjectRepository projectRepository,
    IProjectMetadataRepository projectMetadataRepository,
    ICharacterRepository characterRepository,
    IAdvSenderFactory advSenderFactory,
    ICharacterUriLocator characterUriLocator,
    ILogger<AdvertisementJob> logger) : IDailyJob
{
    private const int MinOtherAdvertisementsBetweenRepeats = 3;
    private const int MaxProjectsInDigest = 10;

    public async Task RunOnce(CancellationToken cancellationToken)
    {
        var schedules = await scheduleRepository.GetActiveSchedules();
        if (schedules.Count == 0)
        {
            logger.LogInformation("Активных расписаний рекламы нет — джоба ничего не делает");
            return;
        }

        var today = DateTime.UtcNow.DayOfWeek;
        foreach (var schedule in schedules)
        {
            if (!schedule.Days.Contains(today))
            {
                logger.LogInformation(
                    "Расписание {scheduleId}: сегодня ({today}) не входит в дни рассылки {days}, пропускаем",
                    schedule.ScheduleId, today, schedule.Days);
                continue;
            }

            switch (schedule.Method)
            {
                case AdvertisementMethod.SingleHotRole:
                    var sender = advSenderFactory.Create(schedule.Channel);
                    if (sender is null)
                    {
                        logger.LogWarning(
                            "Расписание {scheduleId}: канал {channelId} не поддерживается (неизвестный вид или некорректные настройки), пропускаем",
                            schedule.ScheduleId, schedule.Channel.ChannelId);
                        break;
                    }

                    if (schedule.Channel.BoundProjectId is not null)
                    {
                        await ProcessProjectBoundScheduleSingleHotRole(schedule, sender);
                    }
                    else
                    {
                        await ProcessCommonSingleHotRole(schedule, sender);
                    }
                    break;
                case AdvertisementMethod.NewlyOpenedProjectsDigest:
                    var digestSender = advSenderFactory.CreateNewlyOpenedProjectsDigestSender(schedule.Channel);
                    if (digestSender is null)
                    {
                        logger.LogWarning(
                            "Расписание {scheduleId}: канал {channelId} не поддерживается (неизвестный вид или некорректные настройки), пропускаем",
                            schedule.ScheduleId, schedule.Channel.ChannelId);
                        break;
                    }

                    await ProcessNewlyOpenedProjectsDigest(schedule, digestSender);
                    break;
                default:
                    logger.LogWarning(
                        "Расписание {scheduleId}: метод {method} не реализован, пропускаем",
                        schedule.ScheduleId, schedule.Method);
                    break;
            }
        }
    }

    private async Task ProcessProjectBoundScheduleSingleHotRole(AdvertisementScheduleInfo schedule, ISingleHotRoleSender sender)
    {
        var projectId = schedule.Channel.BoundProjectId!;

        logger.LogInformation(
            "Расписание {scheduleId} (канал {channelId}): привязано к проекту {projectId}",
            schedule.ScheduleId, schedule.Channel.ChannelId, projectId);

        await TryAdvertiseProject(projectId, schedule, sender);
    }

    private async Task ProcessCommonSingleHotRole(AdvertisementScheduleInfo schedule, ISingleHotRoleSender sender)
    {
        var candidates = await projectRepository.GetPublicProjectsOpenForHotRoleAdvertisement();
        if (candidates.Count == 0)
        {
            logger.LogInformation("Расписание {scheduleId}: нет проектов с горячими ролями для рекламы", schedule.ScheduleId);
            return;
        }

        foreach (var candidate in AdvertisementGameRanking.OrderByPriority(candidates))
        {
            logger.LogInformation(
                "Расписание {scheduleId} (канал {channelId}): наиболее приоритетный проект {projectId}",
                schedule.ScheduleId, schedule.Channel.ChannelId, candidate.ProjectId);

            if (await TryAdvertiseProject(candidate.ProjectId, schedule, sender))
            {
                return;
            }
            // проект не подошёл (нет карточки КогдаИгры в будущем или не осталось горячих ролей) — пробуем следующий по приоритету
        }

        logger.LogInformation("Расписание {scheduleId}: не нашлось ни одной подходящей роли ни в одном проекте", schedule.ScheduleId);
    }

    /// <returns>true, если для проекта была выбрана и отправлена роль; false — если проект не подошёл (не открыт для рекламы, нет карточки КогдаИгры в будущем или горячих ролей).</returns>
    private async Task<bool> TryAdvertiseProject(ProjectIdentification projectId, AdvertisementScheduleInfo schedule, ISingleHotRoleSender sender)
    {
        if (await ValidateForAdvertisement(projectId, schedule) is not { } details)
        {
            return false;
        }

        var characterId = await TrySelectSingleHotRoleForProject(projectId, schedule);
        if (characterId is null)
        {
            logger.LogInformation(
                "Расписание {scheduleId}: в проекте {projectId} не осталось подходящих горячих ролей",
                schedule.ScheduleId, projectId);
            return false;
        }

        logger.LogInformation(
            "Расписание {scheduleId}: выбрана роль {characterId} в проекте {projectId} для отправки",
            schedule.ScheduleId, characterId, projectId);

        await SendSingleHotRole(characterId, details.ProjectInfo, details, sender, schedule);
        return true;
    }

    /// <summary>
    /// Общие проверки перед рекламой проекта — используются и для одиночной горячей роли, и для
    /// дайджеста: проект сейчас публичный и открыт для заявок, не на кулдауне в этом расписании,
    /// есть карточка КогдаИгры в будущем.
    /// </summary>
    private async Task<DomainTypes.ProjectMetadata.ProjectDetails?> ValidateForAdvertisement(
        ProjectIdentification projectId, AdvertisementScheduleInfo schedule)
    {
        var details = await projectMetadataRepository.GetProjectDetails(projectId);
        var projectInfo = details.ProjectInfo;
        if (projectInfo.ProjectStatus != ProjectLifecycleStatus.ActiveClaimsOpen || !projectInfo.ClaimSettings.IsPublicProject)
        {
            logger.LogInformation(
                "Расписание {scheduleId}: проект {projectId} сейчас не открыт для рекламы (не публичный или не принимает заявки), пропускаем",
                schedule.ScheduleId, projectId);
            return null;
        }

        if (await logRepository.WasProjectAdvertisedAmongLastN(schedule.ScheduleId, projectId, MinOtherAdvertisementsBetweenRepeats))
        {
            logger.LogInformation(
                "Расписание {scheduleId}: проект {projectId} на кулдауне (рекламировался среди последних {n} реклам в канале), пропускаем",
                schedule.ScheduleId, projectId, MinOtherAdvertisementsBetweenRepeats);
            return null;
        }

        if (details.NearestFutureKogdaIgraCard is null)
        {
            logger.LogInformation(
                "Расписание {scheduleId}: у проекта {projectId} нет карточки КогдаИгры в будущем (привязано карточек: {cardCount}), пропускаем",
                schedule.ScheduleId, projectId, details.KogdaIgraCards.Count);
            return null;
        }

        return details;
    }

    private async Task ProcessNewlyOpenedProjectsDigest(AdvertisementScheduleInfo schedule, INewlyOpenedProjectsDigestSender sender)
    {
        var candidates = await projectRepository.GetPublicProjectsOpenedForClaimsInLastWeek();

        var digest = new List<(ProjectAdvertisementCandidate Project, KogdaIgraGameData Game)>();
        foreach (var candidate in AdvertisementGameRanking.OrderByPriority(candidates))
        {
            if (digest.Count >= MaxProjectsInDigest)
            {
                break;
            }

            if (await ValidateForAdvertisement(candidate.ProjectId, schedule) is not { } details)
            {
                continue;
            }

            digest.Add((candidate, details.NearestFutureKogdaIgraCard!));
        }

        if (digest.Count == 0)
        {
            logger.LogInformation("Расписание {scheduleId}: нет недавно открывшихся проектов для дайджеста", schedule.ScheduleId);
            return;
        }

        logger.LogInformation(
            "Расписание {scheduleId} (канал {channelId}): отправка дайджеста из {count} проектов",
            schedule.ScheduleId, schedule.Channel.ChannelId, digest.Count);

        var result = await sender.Send(digest);
        var status = result.Succeeded ? AdvertisementLogStatus.Sent : AdvertisementLogStatus.Failed;

        await logRepository.RecordAdvertisementBatch(
            schedule.ScheduleId, schedule.Method, status, DateTimeOffset.UtcNow,
            [.. digest.Select(d => d.Project.ProjectId)]);
    }

    private async Task<CharacterIdentification?> TrySelectSingleHotRoleForProject(ProjectIdentification projectId, AdvertisementScheduleInfo schedule)
    {
        var roles = await logRepository.GetHotCharactersAdvertisementInfo(schedule.ScheduleId, projectId);
        var candidates = roles.Where(r => !r.AlreadySentForSchedule).ToList();

        logger.LogInformation(
            "Проект {projectId}: {totalCount} горячих ролей, {candidateCount} ещё не рекламировались по расписанию {scheduleId}",
            projectId, roles.Count, candidates.Count, schedule.ScheduleId);

        return HotRoleSelector.SelectLeastAdvertised(candidates)?.Character.CharacterId;
    }

    private async Task SendSingleHotRole(
        CharacterIdentification characterId, ProjectInfo projectInfo, DomainTypes.ProjectMetadata.ProjectDetails details, ISingleHotRoleSender sender, AdvertisementScheduleInfo schedule)
    {
        var character = await characterRepository.GetCharacterAsync(characterId);
        var result = await sender.Send(projectInfo, details, character, characterUriLocator.GetAddClaimUri(characterId));
        var status = result.Succeeded ? AdvertisementLogStatus.Sent : AdvertisementLogStatus.Failed;

        logger.LogInformation(
            "Расписание {scheduleId}: отправка роли {characterId} — {status}",
            schedule.ScheduleId, characterId, status);

        await logRepository.RecordAdvertisement(new AdvertisementLogEntryInfo(
            schedule.ScheduleId, schedule.Method, characterId.ProjectId, characterId, status, DateTimeOffset.UtcNow));
    }
}
