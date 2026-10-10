namespace JoinRpg.Services.Impl;

/// <inheritdoc />
internal class UserMergeService(
    IUserMergeRepository userMergeRepository,
    IUserRepository userRepository,
    IVirtualUsersService virtualUsersService,
    ICurrentUserAccessor currentUserAccessor) : IUserMergeService
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
