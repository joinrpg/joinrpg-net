using JoinRpg.Data.Write.Interfaces;

namespace JoinRpg.Services.Impl;

/// <inheritdoc />
internal class UserMergeService(
    IUserMergeRepository userMergeRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IVirtualUsersService virtualUsersService,
    ICurrentUserAccessor currentUserAccessor,
    UserMergeNotifier notifier,
    ILogger<UserMergeService> logger) : IUserMergeService
{
    public async Task<UserMergePreview> GetPreview(UserIdentification primary, UserIdentification secondary)
    {
        if (!currentUserAccessor.IsAdmin)
        {
            throw new MustBeAdminException();
        }

        // Оба аккаунта обязаны существовать: иначе предпросмотр показал бы «конфликтов нет» по пустоте.
        _ = await userRepository.GetRequiredUserInfoHeaders([.. new[] { primary, secondary }.Distinct()]);

        var accountConflicts = GetAccountConflicts(primary, secondary).ToList();
        if (accountConflicts.Count > 0)
        {
            // Дальше смотреть нет смысла: слить нельзя, а для служебных пользователей данные огромны.
            return new UserMergePreview(primary, secondary, accountConflicts, [], new(0, 0, 0));
        }

        return await userMergeRepository.GetPreview(primary, secondary);
    }

    public async Task Merge(UserIdentification primary, UserIdentification secondary)
    {
        var preview = await GetPreview(primary, secondary);
        if (!preview.CanMerge)
        {
            throw new UserMergeConflictException(preview);
        }

        var usersRepository = unitOfWork.GetUsersRepository();
        var primaryUser = await usersRepository.WithProfile(primary.Value);
        var secondaryUser = await usersRepository.WithProfile(secondary.Value);
        var headers = (await userRepository.GetRequiredUserInfoHeaders([primary, secondary])).ToDictionary(h => h.UserId);

        // Поглощаемому — до удаления: после него адрес по id уже не найти. Если слияние сорвётся
        // по любой причине (гонка с конфликтом, таймаут, обрыв соединения), письмо окажется ложным —
        // это осознанная плата за простоту. Недоступная очередь уведомлений, наоборот, не даст начать слияние.
        await notifier.NotifySecondary(headers[secondary], secondaryUser.Email, primaryUser.Email);

        IReadOnlyList<string> profileFieldsTaken = [];
        var affected = await unitOfWork.GetUserMergeWriteRepository().MergeAndDelete(
            primary, secondary, () => profileFieldsTaken = UserProfileMerger.MergeInto(primaryUser, secondaryUser));

        logger.LogWarning(
            "Пользователь {SecondaryUserId} ({SecondaryEmail}) слит в {PrimaryUserId} ({PrimaryEmail}) админом {AdminUserId}. Перенесено: {@Affected}",
            secondary.Value, secondaryUser.Email, primary.Value, primaryUser.Email, currentUserAccessor.UserId,
            affected.Where(x => x.Value > 0).ToDictionary());

        // Слияние уже закоммичено: падение писем не должно выглядеть для админа как сорванное слияние.
        try
        {
            await notifier.NotifyPrimary(headers[primary], secondaryUser.Email, primaryUser.Email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось поставить письмо о слиянии пользователю {PrimaryUserId}", primary.Value);
        }
        try
        {
            await notifier.NotifyAdmins(new UserMergeReport(
                headers[primary], primaryUser.Email, headers[secondary], secondaryUser.Email,
                currentUserAccessor.UserIdentification, affected, profileFieldsTaken, preview.DroppedLoginProviders));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось поставить админам отчёт о слиянии {SecondaryUserId} в {PrimaryUserId}", secondary.Value, primary.Value);
        }
    }

    private IEnumerable<UserMergeConflict> GetAccountConflicts(UserIdentification primary, UserIdentification secondary)
    {
        if (primary == secondary)
        {
            yield return new UserMergeConflict.SameUser();
        }

        // Иначе сессия админа после слияния укажет на удалённого пользователя.
        if (secondary == currentUserAccessor.UserIdentification)
        {
            yield return new UserMergeConflict.SecondaryIsCurrentUser();
        }

        UserIdentification[] virtualUsers =
            [new(virtualUsersService.RobotUser.UserId), new(virtualUsersService.PaymentsUser.UserId)];
        foreach (var userId in new[] { primary, secondary }.Distinct().Where(virtualUsers.Contains))
        {
            yield return new UserMergeConflict.VirtualUser(userId);
        }
    }
}
