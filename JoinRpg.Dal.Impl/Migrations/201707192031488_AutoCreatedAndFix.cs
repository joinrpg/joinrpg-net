namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class AutoCreatedAndFix : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Characters", "AutoCreated", c => c.Boolean(nullable: false, defaultValue: false));

          Sql(
            @"UPDATE Characters SET AutoCreated = 1 FROM Characters C WHERE C.CharacterName LIKE 'Новый персонаж в группе%' AND C.IsActive = 1");

          Sql(@"UPDATE Characters
SET 
ApprovedClaimId = CL.CLaimId
FROM Characters CH
INNER JOIN Claims CL ON CL.CharacterId = CH.CharacterId 
WHERE CL.ClaimStatus = 2 AND (CH.ApprovedClaimId <> CL.CLaimId OR CH.ApprovedClaimId IS NULL)");
        }
        
        public override void Down()
        {
            DropColumn("dbo.Characters", "AutoCreated");
        }
    }
}
