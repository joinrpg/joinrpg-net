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
        if (!context.Set<User>().Any(u => u.UserName == userName))
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
                }
            };
            context.Set<User>().Add(user);
        }
    }
}
