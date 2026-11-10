using System.Data.Entity.Migrations;

namespace JoinRpg.Dal.Impl.Migrations;

public partial class ExpenseMoneyDecimal : DbMigration
{
    public override void Up()
    {
        AlterColumn("dbo.Expenses", "PlannedPrice", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        AlterColumn("dbo.Expenses", "ActualPrice", c => c.Decimal(precision: 18, scale: 2));
    }

    public override void Down()
    {
        AlterColumn("dbo.Expenses", "ActualPrice", c => c.Int());
        AlterColumn("dbo.Expenses", "PlannedPrice", c => c.Int(nullable: false));
    }
}
