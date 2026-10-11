using JoinRpg.Data.Write.Interfaces;

namespace JoinRpg.Services.Impl;

/// <inheritdoc />
internal class UserMergeService(
    IUserMergeRepository userMergeRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IVirtualUsersService virtualUsersService,
    ICurrentUserAccessor currentUserAccessor,
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

        var affected = await unitOfWork.GetUserMergeWriteRepository().MergeAndDelete(
            primary, secondary, () => UserProfileMerger.MergeInto(primaryUser, secondaryUser));

        logger.LogWarning(
            "Пользователь {SecondaryUserId} ({SecondaryEmail}) слит в {PrimaryUserId} ({PrimaryEmail}) админом {AdminUserId}. Перенесено: {@Affected}",
            secondary.Value, secondaryUser.Email, primary.Value, primaryUser.Email, currentUserAccessor.UserId,
            affected.Where(x => x.Value > 0).ToDictionary());
    }

    private IEnumerable<UserMergeConflict> GetAccountConflicts(UserIdentification primary, UserIdentification secondary)
    {
        if (primary == secondary)
        {
            yield return new UserMergeConflict.SameUser();
        }

        UserIdentification[] virtualUsers =
            [new(virtualUsersService.RobotUser.UserId), new(virtualUsersService.PaymentsUser.UserId)];
        foreach (var userId in new[] { primary, secondary }.Distinct().Where(virtualUsers.Contains))
        {
            yield return new UserMergeConflict.VirtualUser(userId);
        }
    }
}
