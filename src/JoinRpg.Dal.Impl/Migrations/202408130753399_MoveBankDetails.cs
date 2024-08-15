namespace JoinRpg.Dal.Impl.Migrations;

using System;
using System.Data.Entity.Migrations;

public partial class MoveBankDetails : DbMigration
{
    public override void Up()
    {
        CreateTable(
            "dbo.FinanceOperationBankDetails",
            c => new
            {
                CommentId = c.Int(nullable: false),
                BankRefundKey = c.String(maxLength: 256),
                BankOperationKey = c.String(maxLength: 256),
                QrCodeLink = c.String(maxLength: 2048),
                QrCodeMeta = c.String(maxLength: 2048),
            })
            .PrimaryKey(t => t.CommentId)
            .ForeignKey("dbo.FinanceOperations", t => t.CommentId)
            .Index(t => t.CommentId);

        DropColumn("dbo.FinanceOperations", "BankRefundKey");
        DropColumn("dbo.FinanceOperations", "BankOperationKey");
    }

    public override void Down()
    {
        AddColumn("dbo.FinanceOperations", "BankOperationKey", c => c.String(maxLength: 256));
        AddColumn("dbo.FinanceOperations", "BankRefundKey", c => c.String(maxLength: 256));
        DropForeignKey("dbo.FinanceOperationBankDetails", "CommentId", "dbo.FinanceOperations");
        DropIndex("dbo.FinanceOperationBankDetails", new[] { "CommentId" });
        DropTable("dbo.FinanceOperationBankDetails");
    }
}
