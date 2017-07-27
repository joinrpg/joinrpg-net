namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class RevertShowOnUnApprovedIncorrectFlag : DbMigration
    {
        public override void Up()
        {
          Sql("UPDATE dbo.ProjectFields SET ShowOnUnApprovedClaims = 1 WHERE FieldBoundTo = 1");
        }
        
        public override void Down()
        {
        }
    }
}
