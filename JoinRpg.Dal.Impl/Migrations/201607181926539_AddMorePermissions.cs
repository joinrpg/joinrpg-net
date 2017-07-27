namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class AddMorePermissions : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ProjectAcls", "CanSendMassMails", c => c.Boolean(nullable: false, defaultValue: true));
            AddColumn("dbo.ProjectAcls", "CanManagePlots", c => c.Boolean(nullable: false, defaultValue: true));
            AddColumn("dbo.ProjectDetails", "PublishPlot", c => c.Boolean(nullable: false, defaultValue: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ProjectDetails", "PublishPlot");
            DropColumn("dbo.ProjectAcls", "CanManagePlots");
            DropColumn("dbo.ProjectAcls", "CanSendMassMails");
        }
    }
}
