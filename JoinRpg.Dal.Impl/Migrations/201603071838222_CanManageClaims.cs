namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class CanManageClaims : DbMigration
    {
        public override void Up()
        {
            RenameColumn("dbo.ProjectAcls", "CanApproveClaims", "CanManageClaims");
        }
        
        public override void Down()
        {
          RenameColumn("dbo.ProjectAcls", "CanManageClaims", "CanApproveClaims");
        }
    }
}
