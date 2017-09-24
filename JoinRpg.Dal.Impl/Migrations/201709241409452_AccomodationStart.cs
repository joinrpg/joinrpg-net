namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AccomodationStart : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ProjectAccomodationTypes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ProjectId = c.Int(nullable: false),
                        Name = c.String(nullable: false),
                        Cost = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Projects", t => t.ProjectId, cascadeDelete: true)
                .Index(t => t.ProjectId);
            
            CreateTable(
                "dbo.ProjectAccomodations",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AccomodationTypeId = c.Int(nullable: false),
                        IsPlayerSelectable = c.Boolean(nullable: false),
                        IsInfinite = c.Boolean(nullable: false),
                        Capacity = c.Int(nullable: false),
                        IsAutofilledAccomodation = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.ProjectAccomodationTypes", t => t.AccomodationTypeId, cascadeDelete: true)
                .Index(t => t.AccomodationTypeId);
            
            AddColumn("dbo.Claims", "ProjectAccomodation_Id", c => c.Int());
            AddColumn("dbo.ProjectAcls", "CanManageAccomodation", c => c.Boolean(nullable: false));
            AddColumn("dbo.ProjectAcls", "CanSetPlayersAccomodations", c => c.Boolean(nullable: false));
            AddColumn("dbo.ProjectDetails", "AccomodationEnabled", c => c.Boolean(nullable: false));
            CreateIndex("dbo.Claims", "ProjectAccomodation_Id");
            AddForeignKey("dbo.Claims", "ProjectAccomodation_Id", "dbo.ProjectAccomodations", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ProjectAccomodations", "AccomodationTypeId", "dbo.ProjectAccomodationTypes");
            DropForeignKey("dbo.Claims", "ProjectAccomodation_Id", "dbo.ProjectAccomodations");
            DropForeignKey("dbo.ProjectAccomodationTypes", "ProjectId", "dbo.Projects");
            DropIndex("dbo.ProjectAccomodations", new[] { "AccomodationTypeId" });
            DropIndex("dbo.ProjectAccomodationTypes", new[] { "ProjectId" });
            DropIndex("dbo.Claims", new[] { "ProjectAccomodation_Id" });
            DropColumn("dbo.ProjectDetails", "AccomodationEnabled");
            DropColumn("dbo.ProjectAcls", "CanSetPlayersAccomodations");
            DropColumn("dbo.ProjectAcls", "CanManageAccomodation");
            DropColumn("dbo.Claims", "ProjectAccomodation_Id");
            DropTable("dbo.ProjectAccomodations");
            DropTable("dbo.ProjectAccomodationTypes");
        }
    }
}
