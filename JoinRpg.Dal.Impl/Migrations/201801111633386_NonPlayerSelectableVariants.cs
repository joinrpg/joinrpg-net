namespace JoinRpg.Dal.Impl.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class NonPlayerSelectableVariants : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ProjectFieldDropdownValues", "PlayerSelectable", c => c.Boolean(nullable: true));
            Sql(@"UPDATE [dbo].[ProjectFieldDropdownValues]
            SET[PlayerSelectable] = Fields.CanPlayerEdit
            FROM[dbo].[ProjectFieldDropdownValues] Variants
                INNER JOIN ProjectFields Fields ON Fields.ProjectFieldId = Variants.ProjectFieldId");
        }

        public override void Down()
        {
            DropColumn("dbo.ProjectFieldDropdownValues", "PlayerSelectable");
        }
    }
}
