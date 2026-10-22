using System.Data.Entity.Migrations;

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
        base.Seed(context);
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
