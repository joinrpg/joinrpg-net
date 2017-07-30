namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class ProjectAnnounce : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ProjectDetails", "ProjectAnnounce_Contents", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.ProjectDetails", "ProjectAnnounce_Contents");
        }
    }
}
