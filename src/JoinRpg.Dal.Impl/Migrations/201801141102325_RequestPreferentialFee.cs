namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class RequestPreferentialFee : DbMigration
{
    public override void Up() => AddColumn("dbo.FinanceOperations", "MarkMeAsPreferential", c => c.Boolean(nullable: false, defaultValue: false));

    public override void Down() => DropColumn("dbo.FinanceOperations", "MarkMeAsPreferential");
}
