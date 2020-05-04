using JoinRpg.DataModel;

namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class FinanceOperation : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.FinanceOperations", "OperationType", c => c.Int(nullable: false));
            AddColumn("dbo.FinanceOperations", "LinkedClaimId", c => c.Int());
            CreateIndex("dbo.FinanceOperations", "LinkedClaimId");
            AddForeignKey("dbo.FinanceOperations", "LinkedClaimId", "dbo.Claims", "ClaimId");

            Sql("UPDATE [dbo].[FinanceOperations] "
                + $"SET OperationType = {(int)FinanceOperationType.PreferentialFeeRequest} "
                + "WHERE MarkMeAsPreferential = 1");
            Sql("UPDATE [dbo].[FinanceOperations] "
                + $"SET OperationType = {(int)FinanceOperationType.Refund} "
                + "WHERE MoneyAmount < 0");
            Sql("UPDATE [dbo].[FinanceOperations] "
                + $"SET OperationType = {(int)FinanceOperationType.Submit} "
                + "WHERE MoneyAmount > 0");
            Sql("UPDATE [dbo].[FinanceOperations] "
                + $"SET OperationType = {(int)FinanceOperationType.FeeChange} "
                + "WHERE FeeChange <> 0");

            DropColumn("dbo.FinanceOperations", "MarkMeAsPreferential");
        }

        public override void Down()
        {
            AddColumn("dbo.FinanceOperations", "MarkMeAsPreferential", c => c.Boolean(nullable: false));

            Sql("UPDATE [dbo].[FinanceOperations]"
                + "SET MarkMeAsPreferential = 1"
                + $"WHERE OperationType = {(int)FinanceOperationType.PreferentialFeeRequest}");

            DropForeignKey("dbo.FinanceOperations", "LinkedClaimId", "dbo.Claims");
            DropIndex("dbo.FinanceOperations", new[] { "LinkedClaimId" });
            DropColumn("dbo.FinanceOperations", "LinkedClaimId");
            DropColumn("dbo.FinanceOperations", "OperationType");
        }
    }
}
