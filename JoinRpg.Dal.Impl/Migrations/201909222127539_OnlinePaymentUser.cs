using JoinRpg.DataModel;

namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class OnlinePaymentUser : DbMigration
    {
        public override void Up()
        {
            User vu = User.CreateVirtualOnlinePaymentsManager();
            var query =
                @"INSERT INTO [dbo].[Users] "
                + $@"({nameof(User.UserName)}, {nameof(User.PrefferedName)}, {nameof(User.Email)}) "
                + $@"VALUES ('{vu.UserName}', '{vu.PrefferedName}', '{vu.Email}')";
            Sql(query);
        }
        
        public override void Down()
        {
        }
    }
}
