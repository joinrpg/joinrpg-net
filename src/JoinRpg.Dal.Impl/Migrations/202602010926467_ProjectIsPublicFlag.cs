namespace JoinRpg.Dal.Impl.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class ProjectIsPublicFlag : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ProjectDetails", "IsPublicProject", c => c.Boolean(nullable: false, defaultValue: true));
        }

        public override void Down()
        {
            DropColumn("dbo.ProjectDetails", "IsPublicProject");
        }
    }
}
