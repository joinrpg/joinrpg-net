namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class Finance6 : DbMigration
{
    public override void Up()
    {
        DropForeignKey("dbo.PaymentTypes", "UserId", "dbo.Users");
        DropIndex("dbo.PaymentTypes", new[] { "UserId" });
        AlterColumn("dbo.PaymentTypes", "UserId", c => c.Int(nullable: false));
        CreateIndex("dbo.PaymentTypes", "UserId");
        AddForeignKey("dbo.PaymentTypes", "UserId", "dbo.Users", "UserId", cascadeDelete: true);
    }

    public override void Down()
    {
        DropForeignKey("dbo.PaymentTypes", "UserId", "dbo.Users");
        DropIndex("dbo.PaymentTypes", new[] { "UserId" });
        AlterColumn("dbo.PaymentTypes", "UserId", c => c.Int());
        CreateIndex("dbo.PaymentTypes", "UserId");
        AddForeignKey("dbo.PaymentTypes", "UserId", "dbo.Users", "UserId");
    }
}
