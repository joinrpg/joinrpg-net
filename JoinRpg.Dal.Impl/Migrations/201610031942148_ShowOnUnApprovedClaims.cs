namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class ShowOnUnApprovedClaims : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ProjectFields", "ShowOnUnApprovedClaims", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ProjectFields", "ShowOnUnApprovedClaims");
        }
    }
}
