namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class CleanFinanceOperation : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.FinanceOperations", "MasterUserId", "dbo.Users");
            DropIndex("dbo.FinanceOperations", new[] { "MasterUserId" });
            DropColumn("dbo.FinanceOperations", "MasterUserId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.FinanceOperations", "MasterUserId", c => c.Int(nullable: false));
            CreateIndex("dbo.FinanceOperations", "MasterUserId");
            AddForeignKey("dbo.FinanceOperations", "MasterUserId", "dbo.Users", "UserId", cascadeDelete: true);
        }
    }
}
