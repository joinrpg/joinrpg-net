namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class ProjectAllrpgLink : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ProjectDetails", "AllrpgId", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.ProjectDetails", "AllrpgId");
        }
    }
}
