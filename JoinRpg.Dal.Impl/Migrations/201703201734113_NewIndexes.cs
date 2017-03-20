namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class NewIndexes : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.Claims", new[] { "ProjectId" });
            DropIndex("dbo.Characters", new[] { "ProjectId" });
            CreateIndex("dbo.Claims", new[] { "ProjectId", "ClaimStatus" }, name: "IX_Claim_Project_Status");
            CreateIndex("dbo.Characters", new[] { "ProjectId", "IsActive" }, name: "IX_Character_Project_Status");
        }
        
        public override void Down()
        {
            DropIndex("dbo.Characters", "IX_Character_Project_Status");
            DropIndex("dbo.Claims", "IX_Claim_Project_Status");
            CreateIndex("dbo.Characters", "ProjectId");
            CreateIndex("dbo.Claims", "ProjectId");
        }
    }
}
