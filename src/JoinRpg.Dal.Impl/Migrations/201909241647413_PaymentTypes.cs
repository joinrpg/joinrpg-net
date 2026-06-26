namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;

public partial class PaymentTypes : DbMigration
{
    public override void Up()
    {
        AddColumn("dbo.PaymentTypes", "TypeKind", c => c.Int(nullable: false));
        Sql($@"
UPDATE [dbo].[PaymentTypes]
SET TypeKind = {(int)PaymentTypeKind.Cash}
WHERE IsCash = 1");
        DropColumn("dbo.PaymentTypes", "IsCash");
    }

    public override void Down()
    {
        AddColumn("dbo.PaymentTypes", "IsCash", c => c.Boolean(nullable: false));
        Sql($@"
UPDATE [dbo].[PaymentTypes]
SET IsCash = 1
WHERE TypeKind = {(int)PaymentTypeKind.Cash}");
        DropColumn("dbo.PaymentTypes", "TypeKind");
    }
}
