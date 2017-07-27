namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class Finances : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.FinanceOperations",
                c => new
                    {
                        FinanceOperationId = c.Int(nullable: false),
                        ProjectId = c.Int(nullable: false),
                        ClaimId = c.Int(nullable: false),
                        FeeChange = c.Int(nullable: false),
                        MoneyAmount = c.Int(nullable: false),
                        PaymentTypeId = c.Int(),
                        MasterUserId = c.Int(nullable: false),
                        CommentId = c.Int(nullable: false),
                        Created = c.DateTime(nullable: false),
                        Changed = c.DateTime(nullable: false),
                        State = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.FinanceOperationId)
                .ForeignKey("dbo.Claims", t => t.ClaimId, cascadeDelete: true)
                .ForeignKey("dbo.Users", t => t.MasterUserId, cascadeDelete: true)
                .ForeignKey("dbo.PaymentTypes", t => t.PaymentTypeId)
                .ForeignKey("dbo.Projects", t => t.ProjectId, cascadeDelete: true)
                .ForeignKey("dbo.Comments", t => t.FinanceOperationId)
                .Index(t => t.FinanceOperationId)
                .Index(t => t.ProjectId)
                .Index(t => t.ClaimId)
                .Index(t => t.PaymentTypeId)
                .Index(t => t.MasterUserId);
            
            CreateTable(
                "dbo.PaymentTypes",
                c => new
                    {
                        PaymentTypeId = c.Int(nullable: false, identity: true),
                        ProjectId = c.Int(nullable: false),
                        Name = c.String(),
                        UserId = c.Int(),
                        IsCash = c.Boolean(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.PaymentTypeId)
                .ForeignKey("dbo.Projects", t => t.ProjectId, cascadeDelete: true)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.ProjectId)
                .Index(t => t.UserId);
            
            AddColumn("dbo.Claims", "CurrentFee", c => c.Int());
            AddColumn("dbo.ProjectAcls", "CanEditRoles", c => c.Boolean(nullable: false));
            AddColumn("dbo.ProjectAcls", "CanAcceptCash", c => c.Boolean(nullable: false));
            AddColumn("dbo.ProjectAcls", "CanManageMoney", c => c.Boolean(nullable: false));
            AddColumn("dbo.ProjectDetails", "DefaultFee", c => c.Int());
            AddColumn("dbo.Comments", "FinanceOperationId", c => c.Int());
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.FinanceOperations", "FinanceOperationId", "dbo.Comments");
            DropForeignKey("dbo.FinanceOperations", "ProjectId", "dbo.Projects");
            DropForeignKey("dbo.PaymentTypes", "UserId", "dbo.Users");
            DropForeignKey("dbo.PaymentTypes", "ProjectId", "dbo.Projects");
            DropForeignKey("dbo.FinanceOperations", "PaymentTypeId", "dbo.PaymentTypes");
            DropForeignKey("dbo.FinanceOperations", "MasterUserId", "dbo.Users");
            DropForeignKey("dbo.FinanceOperations", "ClaimId", "dbo.Claims");
            DropIndex("dbo.PaymentTypes", new[] { "UserId" });
            DropIndex("dbo.PaymentTypes", new[] { "ProjectId" });
            DropIndex("dbo.FinanceOperations", new[] { "MasterUserId" });
            DropIndex("dbo.FinanceOperations", new[] { "PaymentTypeId" });
            DropIndex("dbo.FinanceOperations", new[] { "ClaimId" });
            DropIndex("dbo.FinanceOperations", new[] { "ProjectId" });
            DropIndex("dbo.FinanceOperations", new[] { "FinanceOperationId" });
            DropColumn("dbo.Comments", "FinanceOperationId");
            DropColumn("dbo.ProjectDetails", "DefaultFee");
            DropColumn("dbo.ProjectAcls", "CanManageMoney");
            DropColumn("dbo.ProjectAcls", "CanAcceptCash");
            DropColumn("dbo.ProjectAcls", "CanEditRoles");
            DropColumn("dbo.Claims", "CurrentFee");
            DropTable("dbo.PaymentTypes");
            DropTable("dbo.FinanceOperations");
        }
    }
}
