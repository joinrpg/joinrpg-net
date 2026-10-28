using System.Data.Entity.Migrations;
using JoinRpg.Domain;

namespace JoinRpg.Dal.Impl.Migrations;

public sealed class Configuration : DbMigrationsConfiguration<MyDbContext>
{
    public Configuration()
    {
        AutomaticMigrationsEnabled = false;
        CommandTimeout = 60 * 60; // 1 hour
    }

    protected override void Seed(MyDbContext context)
    {
        EnsureUserExists(context, User.OnlinePaymentVirtualUser, "Online payments");
        EnsureUserExists(context, User.RobotVirtualUser, "Robot", isAdmin: true);

        // Аватарка есть у каждого пользователя (ADR026), виртуальные — не исключение. Заводится отдельным
        // сохранением: за один SaveChanges EF6 не вставит пользователя и аватарку, которые ссылаются друг на друга.
        _ = context.SaveChanges();
        EnsureDefaultAvatar(context, User.OnlinePaymentVirtualUser);
        EnsureDefaultAvatar(context, User.RobotVirtualUser);

        base.Seed(context);
    }

    /// <summary>
    /// Сид выполняется при каждой миграции: аватарку получает только пользователь без выбранной,
    /// повторный прогон ничего не добавляет.
    /// </summary>
    private static void EnsureDefaultAvatar(MyDbContext context, string userName)
    {
        var user = context.Set<User>().Include(u => u.Avatars).Single(u => u.UserName == userName);
        user.EnsureDefaultAvatar();
    }

    private static void EnsureUserExists(MyDbContext context, string userName, string prefferedName, bool isAdmin = false)
    {
        var existingUserId = context.Set<User>().Where(u => u.UserName == userName).Select(u => (int?)u.UserId).SingleOrDefault();
        if (existingUserId is null)
        {
            var user = new User()
            {
                UserName = userName,
                Email = userName,
                PrefferedName = prefferedName,
                VerifiedProfileFlag = true,
                Auth = new UserAuthDetails()
                {
                    EmailConfirmed = true,
                    RegisterDate = DateTime.UtcNow,
                    AspNetSecurityStamp = Guid.NewGuid().ToString(),
                    IsAdmin = isAdmin,
                },
                Extra = new UserExtra(),
            };
            context.Set<User>().Add(user);
        }
        else if (!context.Set<UserExtra>().Any(e => e.UserId == existingUserId))
        {
            // Раньше сид создавал виртуальных пользователей без UserExtra, хотя модель требует её
            // у каждого пользователя. На таком роботе падали уведомления всем админам (#5807).
            context.Set<UserExtra>().Add(new UserExtra() { UserId = existingUserId.Value });
        }
    }
}
