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
                "dbo.AccommodationRequests",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ProjectId = c.Int(nullable: false),
                    AccommodationTypeId = c.Int(nullable: false),
                    AccommodationId = c.Int(),
                    IsAccepted = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.ProjectAccommodations", t => t.AccommodationId)
                .ForeignKey("dbo.ProjectAccommodationTypes", t => t.AccommodationTypeId, cascadeDelete: true)
                .ForeignKey("dbo.Projects", t => t.ProjectId, cascadeDelete: false)
                .Index(t => t.ProjectId)
                .Index(t => t.AccommodationTypeId)
                .Index(t => t.AccommodationId);

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
                .ForeignKey("dbo.Projects", t => t.ProjectId, cascadeDelete: false)
                .ForeignKey("dbo.ProjectAccommodationTypes", t => t.AccommodationTypeId, cascadeDelete: true)
                .Index(t => t.AccommodationTypeId)
                .Index(t => t.ProjectId);

            CreateTable(
                "dbo.AccommodationInvites",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ProjectId = c.Int(nullable: false),
                    FromClaimId = c.Int(nullable: false),
                    ToClaimId = c.Int(nullable: false),
                    IsAccepted = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Claims", t => t.FromClaimId, cascadeDelete: true)
                .ForeignKey("dbo.Projects", t => t.ProjectId, cascadeDelete: false)
                .ForeignKey("dbo.Claims", t => t.ToClaimId, cascadeDelete: false)
                .Index(t => t.ProjectId)
                .Index(t => t.FromClaimId)
                .Index(t => t.ToClaimId);

            AddColumn("dbo.Claims", "AccommodationRequest_Id", c => c.Int());
            AddColumn("dbo.ProjectAcls", "CanManageAccommodation", c => c.Boolean(nullable: false));
            AddColumn("dbo.ProjectAcls", "CanSetPlayersAccommodations", c => c.Boolean(nullable: false));
            AddColumn("dbo.ProjectDetails", "EnableAccommodation", c => c.Boolean(nullable: false));
            CreateIndex("dbo.Claims", "AccommodationRequest_Id");
            AddForeignKey("dbo.Claims", "AccommodationRequest_Id", "dbo.AccommodationRequests", "Id");
        }

        public override void Down()
        {
            DropForeignKey("dbo.AccommodationInvites", "ToClaimId", "dbo.Claims");
            DropForeignKey("dbo.AccommodationInvites", "ProjectId", "dbo.Projects");
            DropForeignKey("dbo.AccommodationInvites", "FromClaimId", "dbo.Claims");
            DropForeignKey("dbo.ProjectAccommodationTypes", "ProjectId", "dbo.Projects");
            DropForeignKey("dbo.Claims", "AccommodationRequest_Id", "dbo.AccommodationRequests");
            DropForeignKey("dbo.AccommodationRequests", "ProjectId", "dbo.Projects");
            DropForeignKey("dbo.AccommodationRequests", "AccommodationTypeId", "dbo.ProjectAccommodationTypes");
            DropForeignKey("dbo.ProjectAccommodations", "AccommodationTypeId", "dbo.ProjectAccommodationTypes");
            DropForeignKey("dbo.ProjectAccommodations", "ProjectId", "dbo.Projects");
            DropForeignKey("dbo.AccommodationRequests", "AccommodationId", "dbo.ProjectAccommodations");
            DropIndex("dbo.AccommodationInvites", new[] { "ToClaimId" });
            DropIndex("dbo.AccommodationInvites", new[] { "FromClaimId" });
            DropIndex("dbo.AccommodationInvites", new[] { "ProjectId" });
            DropIndex("dbo.ProjectAccommodations", new[] { "ProjectId" });
            DropIndex("dbo.ProjectAccommodations", new[] { "AccommodationTypeId" });
            DropIndex("dbo.AccommodationRequests", new[] { "AccommodationId" });
            DropIndex("dbo.AccommodationRequests", new[] { "AccommodationTypeId" });
            DropIndex("dbo.AccommodationRequests", new[] { "ProjectId" });
            DropIndex("dbo.ProjectAccommodationTypes", new[] { "ProjectId" });
            DropIndex("dbo.Claims", new[] { "AccommodationRequest_Id" });
            DropColumn("dbo.ProjectDetails", "EnableAccommodation");
            DropColumn("dbo.ProjectAcls", "CanSetPlayersAccommodations");
            DropColumn("dbo.ProjectAcls", "CanManageAccommodation");
            DropColumn("dbo.Claims", "AccommodationRequest_Id");
            DropTable("dbo.AccommodationInvites");
            DropTable("dbo.ProjectAccommodations");
            DropTable("dbo.AccommodationRequests");
            DropTable("dbo.ProjectAccommodationTypes");
        }
    }
}
