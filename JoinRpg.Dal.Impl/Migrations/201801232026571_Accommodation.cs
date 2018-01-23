namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Accommodation : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ProjectAccommodationTypes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ProjectId = c.Int(nullable: false),
                        Name = c.String(nullable: false),
                        Description = c.String(),
                        Cost = c.Int(nullable: false),
                        Capacity = c.Int(nullable: false),
                        IsInfinite = c.Boolean(nullable: false),
                        IsPlayerSelectable = c.Boolean(nullable: false),
                        IsAutoFilledAccommodation = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Projects", t => t.ProjectId, cascadeDelete: true)
                .Index(t => t.ProjectId);
            
            CreateTable(
                "dbo.ProjectAccommodations",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AccommodationTypeId = c.Int(nullable: false),
                        ProjectId = c.Int(nullable: false),
                        Name = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Projects", t => t.ProjectId, cascadeDelete: true)
                .ForeignKey("dbo.ProjectAccommodationTypes", t => t.AccommodationTypeId, cascadeDelete: false)
                .Index(t => t.AccommodationTypeId)
                .Index(t => t.ProjectId);
            
            AddColumn("dbo.Claims", "ProjectAccommodationType_Id", c => c.Int());
            AddColumn("dbo.Claims", "ProjectAccommodation_Id", c => c.Int());
            AddColumn("dbo.ProjectAcls", "CanManageAccomodation", c => c.Boolean(nullable: false));
            AddColumn("dbo.ProjectAcls", "CanSetPlayersAccomodations", c => c.Boolean(nullable: false));
            AddColumn("dbo.ProjectDetails", "EnableAccomodation", c => c.Boolean(nullable: false));
            CreateIndex("dbo.Claims", "ProjectAccommodationType_Id");
            CreateIndex("dbo.Claims", "ProjectAccommodation_Id");
            AddForeignKey("dbo.Claims", "ProjectAccommodationType_Id", "dbo.ProjectAccommodationTypes", "Id");
            AddForeignKey("dbo.Claims", "ProjectAccommodation_Id", "dbo.ProjectAccommodations", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ProjectAccommodations", "AccommodationTypeId", "dbo.ProjectAccommodationTypes");
            DropForeignKey("dbo.ProjectAccommodations", "ProjectId", "dbo.Projects");
            DropForeignKey("dbo.Claims", "ProjectAccommodation_Id", "dbo.ProjectAccommodations");
            DropForeignKey("dbo.ProjectAccommodationTypes", "ProjectId", "dbo.Projects");
            DropForeignKey("dbo.Claims", "ProjectAccommodationType_Id", "dbo.ProjectAccommodationTypes");
            DropIndex("dbo.ProjectAccommodations", new[] { "ProjectId" });
            DropIndex("dbo.ProjectAccommodations", new[] { "AccommodationTypeId" });
            DropIndex("dbo.ProjectAccommodationTypes", new[] { "ProjectId" });
            DropIndex("dbo.Claims", new[] { "ProjectAccommodation_Id" });
            DropIndex("dbo.Claims", new[] { "ProjectAccommodationType_Id" });
            DropColumn("dbo.ProjectDetails", "EnableAccomodation");
            DropColumn("dbo.ProjectAcls", "CanSetPlayersAccomodations");
            DropColumn("dbo.ProjectAcls", "CanManageAccomodation");
            DropColumn("dbo.Claims", "ProjectAccommodation_Id");
            DropColumn("dbo.Claims", "ProjectAccommodationType_Id");
            DropTable("dbo.ProjectAccommodations");
            DropTable("dbo.ProjectAccommodationTypes");
        }
    }
}
