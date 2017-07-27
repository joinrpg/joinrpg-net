namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class NewIndexes : DbMigration
    {
        public override void Up()
        {
          Sql(@"
CREATE NONCLUSTERED INDEX [nci_wi_Claims_DD27A24D517BFF9AFEC7C6C5C8FCCA84] 
ON [dbo].[Claims] 
([ProjectId], [ClaimStatus]) 
INCLUDE ([CharacterGroupId], [CharacterId], [CommentDiscussionId], [CreateDate], [CurrentFee], [JsonData], [LastUpdateDateTime], [MasterAcceptedDate], 
  [MasterDeclinedDate], [PlayerAcceptedDate], [PlayerDeclinedDate], [PlayerUserId], [ResponsibleMasterUserId]) ");
        }
        
        public override void Down()
        {
          Sql("DROP INDEX [nci_wi_Claims_DD27A24D517BFF9AFEC7C6C5C8FCCA84] ON [dbo].[Claims] ");
        }
    }
}
