namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class VerifiedProfileFlag : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "VerifiedProfileFlag", c => c.Boolean(nullable: false, defaultValue: false));
        }

        public override void Down()
        {
            DropColumn("dbo.Users", "VerifiedProfileFlag");
        }
    }
}
