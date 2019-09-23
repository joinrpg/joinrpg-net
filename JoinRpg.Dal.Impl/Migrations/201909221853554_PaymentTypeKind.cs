using JoinRpg.DataModel;

namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class PaymentTypeKind : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PaymentTypes", "Kind", c => c.Int(nullable: false));
            var query = $@"UPDATE [dbo].[PaymentTypes] SET "
                + $@"Kind = {(int) DataModel.PaymentTypeKind.Cash} "
                + $@"WHERE IsCash = 1";
            Sql(query);
            DropColumn("dbo.PaymentTypes", "IsCash");
        }
        
        public override void Down()
        {
            AddColumn("dbo.PaymentTypes", "IsCash", c => c.Boolean(nullable: false));
            var query = $@"UPDATE [dbo].[PaymentTypes] SET "
                + $@"IsCash = 1 "
                + $@"WHERE Kind = {(int) DataModel.PaymentTypeKind.Cash}";
            Sql(query);
            DropColumn("dbo.PaymentTypes", "Kind");
        }
    }
}
