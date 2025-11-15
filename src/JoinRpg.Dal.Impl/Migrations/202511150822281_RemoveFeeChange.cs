namespace JoinRpg.Dal.Impl.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class RemoveFeeChange : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.FinanceOperations", "FeeChange");
        }

        public override void Down()
        {
            AddColumn("dbo.FinanceOperations", "FeeChange", c => c.Int(nullable: false));
        }
    }
}
