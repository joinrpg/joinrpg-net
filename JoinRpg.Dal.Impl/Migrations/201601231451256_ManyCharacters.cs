namespace JoinRpg.Dal.Impl.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class ManyCharacters : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ProjectDetails", "EnableManyCharacters", c => c.Boolean(nullable: false));
        }

        public override void Down()
        {
            DropColumn("dbo.ProjectDetails", "EnableManyCharacters");
        }
    }
}
