namespace JoinRpg.Dal.Impl.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class Finance2 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ProjectFeeSettings",
                c => new
                {
                    ProjectFeeSettingId = c.Int(nullable: false, identity: true),
                    ProjectId = c.Int(nullable: false),
                    Fee = c.Int(nullable: false),
                    EndDate = c.DateTime(nullable: false),
                })
                .PrimaryKey(t => t.ProjectFeeSettingId)
                .ForeignKey("dbo.Projects", t => t.ProjectId, cascadeDelete: true)
                .Index(t => t.ProjectId);

            DropColumn("dbo.ProjectDetails", "DefaultFee");
        }

        public override void Down()
        {
            AddColumn("dbo.ProjectDetails", "DefaultFee", c => c.Int());
            DropForeignKey("dbo.ProjectFeeSettings", "ProjectId", "dbo.Projects");
            DropIndex("dbo.ProjectFeeSettings", new[] { "ProjectId" });
            DropTable("dbo.ProjectFeeSettings");
        }
    }
}
