namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class HidePlayerForCharacter : DbMigration
    {
        public override void Up()
        {
          AddColumn("dbo.Characters", "HidePlayerForCharacter", c => c.Boolean(nullable: false, defaultValue: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Characters", "HidePlayerForCharacter");
        }
    }
}
