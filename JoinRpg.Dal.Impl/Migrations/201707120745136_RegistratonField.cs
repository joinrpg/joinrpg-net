namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class RegistratonField : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Claims", "CheckInDate", c => c.DateTime());
            AddColumn("dbo.Characters", "InGame", c => c.Boolean(nullable: false));
            AddColumn("dbo.ProjectDetails", "EnableCheckInModule", c => c.Boolean(nullable: false));
            AddColumn("dbo.ProjectDetails", "CheckInProgress", c => c.Boolean(nullable: false));
            AddColumn("dbo.ProjectDetails", "AllowSecondRoles", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ProjectDetails", "AllowSecondRoles");
            DropColumn("dbo.ProjectDetails", "CheckInProgress");
            DropColumn("dbo.ProjectDetails", "EnableCheckInModule");
            DropColumn("dbo.Characters", "InGame");
            DropColumn("dbo.Claims", "CheckInDate");
        }
    }
}
