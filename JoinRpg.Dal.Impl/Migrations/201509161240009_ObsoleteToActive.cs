namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class ObsoleteToActive : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PlotElements", "IsActive", c => c.Boolean(nullable: false, defaultValue: true));
            AddColumn("dbo.PlotFolders", "IsActive", c => c.Boolean(nullable: false, defaultValue: true));
            DropColumn("dbo.PlotElements", "IsObsolete");
            DropColumn("dbo.PlotFolders", "IsObsolete");
        }
        
        public override void Down()
        {
            AddColumn("dbo.PlotFolders", "IsObsolete", c => c.Boolean(nullable: false));
            AddColumn("dbo.PlotElements", "IsObsolete", c => c.Boolean(nullable: false));
            DropColumn("dbo.PlotFolders", "IsActive");
            DropColumn("dbo.PlotElements", "IsActive");
        }
    }
}
