namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class IsHotRoles : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Characters", "IsHot", c => c.Boolean(nullable: false, defaultValue: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Characters", "IsHot");
        }
    }
}
