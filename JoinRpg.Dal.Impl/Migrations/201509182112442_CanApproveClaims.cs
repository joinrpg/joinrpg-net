namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class CanApproveClaims : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ProjectAcls", "CanApproveClaims", c => c.Boolean(nullable: false, defaultValue:true));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ProjectAcls", "CanApproveClaims");
        }
    }
}
