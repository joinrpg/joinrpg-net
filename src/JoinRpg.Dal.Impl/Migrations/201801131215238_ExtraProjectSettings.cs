namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class ExtraProjectSettings : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ProjectDetails", "AutoAcceptClaims", c => c.Boolean(nullable: false));
            AddColumn("dbo.ProjectDetails", "GenerateCharacterNamesFromPlayer", c => c.Boolean(nullable: false));
        }

        public override void Down()
        {
            DropColumn("dbo.ProjectDetails", "GenerateCharacterNamesFromPlayer");
            DropColumn("dbo.ProjectDetails", "AutoAcceptClaims");
        }
    }
}
