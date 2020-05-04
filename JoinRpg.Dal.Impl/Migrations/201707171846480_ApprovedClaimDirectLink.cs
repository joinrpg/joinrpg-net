namespace JoinRpg.Dal.Impl.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class ApprovedClaimDirectLink : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Characters", "ApprovedClaimId", c => c.Int());
            Sql(@"UPDATE Characters 
SET ApprovedClaimId = CL.ClaimId
FROM Characters CH
INNER JOIN Claims CL ON CL.CharacterId = CH.CharacterId
WHERE ClaimStatus = 2");
            CreateIndex("dbo.Characters", "ApprovedClaimId");
            AddForeignKey("dbo.Characters", "ApprovedClaimId", "dbo.Claims", "ClaimId");
        }

        public override void Down()
        {
            DropForeignKey("dbo.Characters", "ApprovedClaimId", "dbo.Claims");
            DropIndex("dbo.Characters", new[] { "ApprovedClaimId" });
            DropColumn("dbo.Characters", "ApprovedClaimId");
        }
    }
}
