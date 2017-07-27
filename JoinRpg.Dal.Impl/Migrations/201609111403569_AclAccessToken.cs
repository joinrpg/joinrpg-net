namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class AclAccessToken : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ProjectAcls", "Token", c => c.Guid(nullable: false, defaultValueSql: "NEWID()"));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ProjectAcls", "Token");
        }
    }
}
