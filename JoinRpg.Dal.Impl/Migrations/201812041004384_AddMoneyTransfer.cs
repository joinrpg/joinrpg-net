namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddMoneyTransfer : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.MoneyTransfers",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ProjectId = c.Int(nullable: false),
                        SenderId = c.Int(nullable: false),
                        ReceiverId = c.Int(nullable: false),
                        Amount = c.Int(nullable: false),
                        ResultState = c.Int(nullable: false),
                        Created = c.DateTimeOffset(nullable: false, precision: 7),
                        Changed = c.DateTimeOffset(nullable: false, precision: 7),
                        CreatedById = c.Int(nullable: false),
                        ChangedById = c.Int(nullable: false),
                        OperationDate = c.DateTimeOffset(nullable: false, precision: 7),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.ChangedById)
                .ForeignKey("dbo.Users", t => t.CreatedById)
                .ForeignKey("dbo.Projects", t => t.ProjectId, cascadeDelete: true)
                .ForeignKey("dbo.Users", t => t.ReceiverId)
                .ForeignKey("dbo.Users", t => t.SenderId)
                .Index(t => t.ProjectId)
                .Index(t => t.SenderId)
                .Index(t => t.ReceiverId)
                .Index(t => t.CreatedById)
                .Index(t => t.ChangedById);
            
            CreateTable(
                "dbo.TransferTexts",
                c => new
                    {
                        MoneyTransferId = c.Int(nullable: false),
                        Text_Contents = c.String(),
                    })
                .PrimaryKey(t => t.MoneyTransferId)
                .ForeignKey("dbo.MoneyTransfers", t => t.MoneyTransferId)
                .Index(t => t.MoneyTransferId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.TransferTexts", "MoneyTransferId", "dbo.MoneyTransfers");
            DropForeignKey("dbo.MoneyTransfers", "SenderId", "dbo.Users");
            DropForeignKey("dbo.MoneyTransfers", "ReceiverId", "dbo.Users");
            DropForeignKey("dbo.MoneyTransfers", "ProjectId", "dbo.Projects");
            DropForeignKey("dbo.MoneyTransfers", "CreatedById", "dbo.Users");
            DropForeignKey("dbo.MoneyTransfers", "ChangedById", "dbo.Users");
            DropIndex("dbo.TransferTexts", new[] { "MoneyTransferId" });
            DropIndex("dbo.MoneyTransfers", new[] { "ChangedById" });
            DropIndex("dbo.MoneyTransfers", new[] { "CreatedById" });
            DropIndex("dbo.MoneyTransfers", new[] { "ReceiverId" });
            DropIndex("dbo.MoneyTransfers", new[] { "SenderId" });
            DropIndex("dbo.MoneyTransfers", new[] { "ProjectId" });
            DropTable("dbo.TransferTexts");
            DropTable("dbo.MoneyTransfers");
        }
    }
}
