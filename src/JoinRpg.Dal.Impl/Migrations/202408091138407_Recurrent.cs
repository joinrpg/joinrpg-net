namespace JoinRpg.Dal.Impl.Migrations;

using System;
using System.Data.Entity.Migrations;

public partial class Recurrent : DbMigration
{
    public override void Up()
    {
        CreateTable(
            "dbo.RecurrentPayments",
            c => new
            {
                RecurrentPaymentId = c.Int(nullable: false, identity: true),
                ProjectId = c.Int(nullable: false),
                ClaimId = c.Int(nullable: false),
                PaymentTypeId = c.Int(nullable: false),
                CreateDate = c.DateTimeOffset(nullable: false, precision: 7),
                CloseDate = c.DateTimeOffset(precision: 7),
                PaymentId = c.Int(nullable: false),
                BankRecurrencyToken = c.String(),
                BankParentPayment = c.String(),
                BankAdditional = c.String(),
                PaymentAmount = c.Int(nullable: false),
                Status = c.Int(nullable: false),
            })
            .PrimaryKey(t => t.RecurrentPaymentId)
            .ForeignKey("dbo.Claims", t => t.ClaimId)
            .ForeignKey("dbo.PaymentTypes", t => t.PaymentTypeId)
            .ForeignKey("dbo.Projects", t => t.ProjectId)
            .Index(t => t.ProjectId)
            .Index(t => t.ClaimId)
            .Index(t => t.PaymentTypeId);

        AddColumn("dbo.FinanceOperations", "RecurrentPaymentId", c => c.Int());
        AddColumn("dbo.FinanceOperations", "ReccurrentPaymentInstanceToken", c => c.String());
        CreateIndex("dbo.FinanceOperations", "RecurrentPaymentId");
        AddForeignKey("dbo.FinanceOperations", "RecurrentPaymentId", "dbo.RecurrentPayments", "RecurrentPaymentId");
    }

    public override void Down()
    {
        DropForeignKey("dbo.FinanceOperations", "RecurrentPaymentId", "dbo.RecurrentPayments");
        DropForeignKey("dbo.RecurrentPayments", "ProjectId", "dbo.Projects");
        DropForeignKey("dbo.RecurrentPayments", "PaymentTypeId", "dbo.PaymentTypes");
        DropForeignKey("dbo.RecurrentPayments", "ClaimId", "dbo.Claims");
        DropIndex("dbo.RecurrentPayments", new[] { "PaymentTypeId" });
        DropIndex("dbo.RecurrentPayments", new[] { "ClaimId" });
        DropIndex("dbo.RecurrentPayments", new[] { "ProjectId" });
        DropIndex("dbo.FinanceOperations", new[] { "RecurrentPaymentId" });
        DropColumn("dbo.FinanceOperations", "ReccurrentPaymentInstanceToken");
        DropColumn("dbo.FinanceOperations", "RecurrentPaymentId");
        DropTable("dbo.RecurrentPayments");
    }
}
