namespace JoinRpg.Dal.Impl.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class MoneyOperationSubscrive : DbMigration
    {
        public override void Up() => AddColumn("dbo.UserSubscriptions", "MoneyOperation", c => c.Boolean(nullable: false, defaultValue: true));

        public override void Down() => DropColumn("dbo.UserSubscriptions", "MoneyOperation");
    }
}
