namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class moreReccurennt : DbMigration
{
    public override void Up()
    {
        RenameColumn("dbo.FinanceOperations", "BankRefundToken", "BankRefundKey");
        AddColumn("dbo.FinanceOperations", "BankOperationKey", c => c.String(maxLength: 256));
        DropColumn("dbo.RecurrentPayments", "BankAdditional");
    }

    public override void Down()
    {
        AddColumn("dbo.RecurrentPayments", "BankAdditional", c => c.String());
        RenameColumn("dbo.FinanceOperations", "BankRefundKey", "BankRefundToken");
        DropColumn("dbo.FinanceOperations", "BankOperationKey");
    }
}
