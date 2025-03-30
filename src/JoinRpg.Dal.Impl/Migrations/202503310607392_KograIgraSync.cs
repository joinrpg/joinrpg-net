namespace JoinRpg.Dal.Impl.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class KograIgraSync : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.KogdaIgraGames",
                c => new
                {
                    KogdaIgraGameId = c.Int(nullable: false),
                    UpdateRequestedAt = c.DateTimeOffset(nullable: false, precision: 7),
                    LastUpdatedAt = c.DateTimeOffset(precision: 7),
                    JsonGameData = c.String(),
                    Name = c.String(),
                })
                .PrimaryKey(t => t.KogdaIgraGameId);

            CreateTable(
                "dbo.KogdaIgraGameProjects",
                c => new
                {
                    KogdaIgraGame_KogdaIgraGameId = c.Int(nullable: false),
                    Project_ProjectId = c.Int(nullable: false),
                })
                .PrimaryKey(t => new { t.KogdaIgraGame_KogdaIgraGameId, t.Project_ProjectId })
                .ForeignKey("dbo.KogdaIgraGames", t => t.KogdaIgraGame_KogdaIgraGameId, cascadeDelete: true)
                .ForeignKey("dbo.Projects", t => t.Project_ProjectId, cascadeDelete: true)
                .Index(t => t.KogdaIgraGame_KogdaIgraGameId)
                .Index(t => t.Project_ProjectId);

        }

        public override void Down()
        {
            DropForeignKey("dbo.KogdaIgraGameProjects", "Project_ProjectId", "dbo.Projects");
            DropForeignKey("dbo.KogdaIgraGameProjects", "KogdaIgraGame_KogdaIgraGameId", "dbo.KogdaIgraGames");
            DropIndex("dbo.KogdaIgraGameProjects", new[] { "Project_ProjectId" });
            DropIndex("dbo.KogdaIgraGameProjects", new[] { "KogdaIgraGame_KogdaIgraGameId" });
            DropTable("dbo.KogdaIgraGameProjects");
            DropTable("dbo.KogdaIgraGames");
        }
    }
}
