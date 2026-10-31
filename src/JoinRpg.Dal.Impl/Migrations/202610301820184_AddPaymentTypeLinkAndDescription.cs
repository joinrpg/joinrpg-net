namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class AddPaymentTypeLinkAndDescription : DbMigration
{
    // 0 — PaymentLinkKind.Redirect: у существующих методов оплаты ссылок нет.
    public override void Up()
    {
        AddColumn("dbo.PaymentTypes", "PaymentLink", c => c.String());
        AddColumn("dbo.PaymentTypes", "LinkKind", c => c.Int(nullable: false, defaultValue: 0));
        AddColumn("dbo.PaymentTypes", "Description", c => c.String());
    }

    public override void Down()
    {
        DropColumn("dbo.PaymentTypes", "Description");
        DropColumn("dbo.PaymentTypes", "LinkKind");
        DropColumn("dbo.PaymentTypes", "PaymentLink");
    }
}
