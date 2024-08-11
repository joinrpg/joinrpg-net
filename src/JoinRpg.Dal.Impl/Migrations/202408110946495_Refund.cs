namespace JoinRpg.Dal.Impl.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class Refund : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.FinanceOperations", "BankRefundToken", c => c.String(maxLength: 256));
            AddColumn("dbo.FinanceOperations", "RefundedOperationId", c => c.Int());
            CreateIndex("dbo.FinanceOperations", "RefundedOperationId");
            AddForeignKey("dbo.FinanceOperations", "RefundedOperationId", "dbo.FinanceOperations", "CommentId");
        }

        public override void Down()
        {
            DropForeignKey("dbo.FinanceOperations", "RefundedOperationId", "dbo.FinanceOperations");
            DropIndex("dbo.FinanceOperations", new[] { "RefundedOperationId" });
            DropColumn("dbo.FinanceOperations", "RefundedOperationId");
            DropColumn("dbo.FinanceOperations", "BankRefundToken");
        }
    }
}
