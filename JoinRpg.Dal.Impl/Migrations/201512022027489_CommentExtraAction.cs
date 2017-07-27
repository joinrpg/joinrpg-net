namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class CommentExtraAction : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Comments", "ExtraAction", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Comments", "ExtraAction");
        }
    }
}
