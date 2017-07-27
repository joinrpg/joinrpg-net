namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class Finance4 : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.FinanceOperations", "CommentId");
            RenameColumn(table: "dbo.FinanceOperations", name: "FinanceOperationId", newName: "CommentId");
            RenameIndex(table: "dbo.FinanceOperations", name: "IX_FinanceOperationId", newName: "IX_CommentId");
        }
        
        public override void Down()
        {
            RenameIndex(table: "dbo.FinanceOperations", name: "IX_CommentId", newName: "IX_FinanceOperationId");
            RenameColumn(table: "dbo.FinanceOperations", name: "CommentId", newName: "FinanceOperationId");
            AddColumn("dbo.FinanceOperations", "CommentId", c => c.Int(nullable: false));
        }
    }
}
