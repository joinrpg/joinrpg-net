namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class Financ5 : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Comments", "FinanceOperationId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Comments", "FinanceOperationId", c => c.Int());
        }
    }
}
