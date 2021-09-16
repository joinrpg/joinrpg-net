namespace JoinRpg.Dal.Impl.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class FieldsOrdering : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Projects", "ProjectFieldsOrdering", c => c.String());
            AddColumn("dbo.ProjectCharacterFields", "ValuesOrdering", c => c.String());
            DropColumn("dbo.ProjectCharacterFields", "Order");
        }

        public override void Down()
        {
            AddColumn("dbo.ProjectCharacterFields", "Order", c => c.Int(nullable: false));
            DropColumn("dbo.ProjectCharacterFields", "ValuesOrdering");
            DropColumn("dbo.Projects", "ProjectFieldsOrdering");
        }
    }
}
