namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class EnableToStartAndStopAcceptingClaims : DbMigration
    {
        public override void Up() 
        {
            AddColumn("dbo.Projects", "IsAcceptingClaims", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Projects", "IsAcceptingClaims");
        }
    }
}
