namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class ValidForNpc : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ProjectFields", "ValidForNpc", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ProjectFields", "ValidForNpc");
        }
    }
}
