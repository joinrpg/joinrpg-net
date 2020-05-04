namespace JoinRpg.Dal.Impl.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class IncludeInPrint : DbMigration
    {
        public override void Up() => AddColumn("dbo.ProjectFields", "IncludeInPrint", c => c.Boolean(nullable: false, defaultValue: true));

        public override void Down() => DropColumn("dbo.ProjectFields", "IncludeInPrint");
    }
}
