namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class RemoveProjectAclTokenGeneration : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.ProjectAcls", "Token", c => c.Guid(nullable: false));
        }

        public override void Down()
        {
            AlterColumn("dbo.ProjectAcls", "Token", c => c.Guid(nullable: false));
        }
    }
}
