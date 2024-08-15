namespace JoinRpg.Dal.Impl.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class RemoveCharacterNameLegacyMode : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.ProjectDetails", "CharacterNameLegacyMode");
        }

        public override void Down()
        {
            AddColumn("dbo.ProjectDetails", "CharacterNameLegacyMode", c => c.Boolean(nullable: false));
        }
    }
}
