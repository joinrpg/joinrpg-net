using System;
using System.Data.Entity.Migrations;
using System.Linq;
using JoinRpg.DataModel;

namespace JoinRpg.Dal.Impl.Migrations
{
    public sealed class Configuration : DbMigrationsConfiguration<MyDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            CommandTimeout = 60 * 60; // 1 hour
        }

        protected override void Seed(MyDbContext context)
        {
            if (!context.Set<User>().Any(u => u.UserName == User.OnlinePaymentVirtualUser))
            {
                var user = new User()
                {
                    UserName = User.OnlinePaymentVirtualUser,
                    Email = User.OnlinePaymentVirtualUser,
                    PrefferedName = "Online payments",
                    VerifiedProfileFlag = true,
                    Auth = new UserAuthDetails()
                    {
                        EmailConfirmed = true,
                        RegisterDate = DateTime.UtcNow,
                        AspNetSecurityStamp = Guid.NewGuid().ToString(),
                    }
                };
                context.Set<User>().Add(user);
            }
            base.Seed(context);
        }
    }
}
