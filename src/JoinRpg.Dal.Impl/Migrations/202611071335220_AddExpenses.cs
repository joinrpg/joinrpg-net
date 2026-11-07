using System.Data.Entity.Migrations;

namespace JoinRpg.Dal.Impl.Migrations;

public partial class AddExpenses : DbMigration
{
    public override void Up()
    {
        CreateTable(
            "dbo.ExpenseCategories",
            c => new
            {
                ExpenseCategoryId = c.Int(nullable: false, identity: true),
                ProjectId = c.Int(nullable: false),
                Name = c.String(nullable: false),
                ParentCategoryId = c.Int(),
                ParentCategoriesImpl_ListIds = c.String(),
                IsActive = c.Boolean(nullable: false),
            })
            .PrimaryKey(t => t.ExpenseCategoryId)
            .ForeignKey("dbo.ExpenseCategories", t => t.ParentCategoryId)
            .ForeignKey("dbo.Projects", t => t.ProjectId, cascadeDelete: true)
            .Index(t => t.ProjectId)
            .Index(t => t.ParentCategoryId);

        CreateTable(
            "dbo.Expenses",
            c => new
            {
                ExpenseId = c.Int(nullable: false, identity: true),
                ProjectId = c.Int(nullable: false),
                Name = c.String(nullable: false),
                Description_Contents = c.String(),
                MasterId = c.Int(nullable: false),
                CategoryId = c.Int(nullable: false),
                CategoryPathImpl_ListIds = c.String(),
                PlannedQuantity = c.Int(nullable: false),
                PlannedPrice = c.Int(nullable: false),
                ActualQuantity = c.Int(),
                ActualPrice = c.Int(),
                SpentAt = c.DateTime(),
                State = c.Int(nullable: false),
                CreatedAt = c.DateTime(nullable: false),
                CreatedById = c.Int(nullable: false),
                UpdatedAt = c.DateTime(nullable: false),
                UpdatedById = c.Int(nullable: false),
            })
            .PrimaryKey(t => t.ExpenseId)
            .ForeignKey("dbo.ExpenseCategories", t => t.CategoryId)
            .ForeignKey("dbo.Users", t => t.CreatedById)
            .ForeignKey("dbo.Users", t => t.MasterId)
            .ForeignKey("dbo.Projects", t => t.ProjectId, cascadeDelete: true)
            .ForeignKey("dbo.Users", t => t.UpdatedById)
            .Index(t => t.ProjectId)
            .Index(t => t.MasterId)
            .Index(t => t.CategoryId)
            .Index(t => t.CreatedById)
            .Index(t => t.UpdatedById);

        CreateTable(
            "dbo.ExpenseApprovals",
            c => new
            {
                ExpenseApprovalId = c.Int(nullable: false, identity: true),
                ExpenseId = c.Int(nullable: false),
                State = c.Int(nullable: false),
                ApprovedById = c.Int(nullable: false),
                Timestamp = c.DateTime(nullable: false),
                Comment_Contents = c.String(),
            })
            .PrimaryKey(t => t.ExpenseApprovalId)
            .ForeignKey("dbo.Users", t => t.ApprovedById)
            .ForeignKey("dbo.Expenses", t => t.ExpenseId, cascadeDelete: true)
            .Index(t => t.ExpenseId)
            .Index(t => t.ApprovedById);
    }

    public override void Down()
    {
        DropForeignKey("dbo.ExpenseApprovals", "ExpenseId", "dbo.Expenses");
        DropForeignKey("dbo.ExpenseApprovals", "ApprovedById", "dbo.Users");
        DropForeignKey("dbo.Expenses", "UpdatedById", "dbo.Users");
        DropForeignKey("dbo.Expenses", "ProjectId", "dbo.Projects");
        DropForeignKey("dbo.Expenses", "MasterId", "dbo.Users");
        DropForeignKey("dbo.Expenses", "CreatedById", "dbo.Users");
        DropForeignKey("dbo.Expenses", "CategoryId", "dbo.ExpenseCategories");
        DropForeignKey("dbo.ExpenseCategories", "ProjectId", "dbo.Projects");
        DropForeignKey("dbo.ExpenseCategories", "ParentCategoryId", "dbo.ExpenseCategories");
        DropIndex("dbo.ExpenseApprovals", new[] { "ApprovedById" });
        DropIndex("dbo.ExpenseApprovals", new[] { "ExpenseId" });
        DropIndex("dbo.Expenses", new[] { "UpdatedById" });
        DropIndex("dbo.Expenses", new[] { "CreatedById" });
        DropIndex("dbo.Expenses", new[] { "CategoryId" });
        DropIndex("dbo.Expenses", new[] { "MasterId" });
        DropIndex("dbo.Expenses", new[] { "ProjectId" });
        DropIndex("dbo.ExpenseCategories", new[] { "ParentCategoryId" });
        DropIndex("dbo.ExpenseCategories", new[] { "ProjectId" });
        DropTable("dbo.ExpenseApprovals");
        DropTable("dbo.Expenses");
        DropTable("dbo.ExpenseCategories");
    }
}
