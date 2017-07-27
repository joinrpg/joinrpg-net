namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class RemoveCashHack : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.ProjectAcls", "CanAcceptCash");
        }
        
        public override void Down()
        {
            AddColumn("dbo.ProjectAcls", "CanAcceptCash", c => c.Boolean(nullable: false));
        }
    }
}
