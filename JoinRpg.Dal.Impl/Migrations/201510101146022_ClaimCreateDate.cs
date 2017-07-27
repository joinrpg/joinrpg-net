namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class ClaimCreateDate : DbMigration
    {
        public override void Up()
        {
          AddColumn("dbo.Claims", "CreateDate", c => c.DateTime(nullable: true));
         Sql("UPDATE [Claims] Set CreateDate = ISNULL(PlayerAcceptedDate, '1-1-1970')");
          AlterColumn("dbo.Claims", "CreateDate", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Claims", "CreateDate");
        }
    }
}
