namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class Finance3 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.FinanceOperations", "OperationDate", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.FinanceOperations", "OperationDate");
        }
    }
}
