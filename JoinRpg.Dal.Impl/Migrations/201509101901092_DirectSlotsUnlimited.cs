namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class DirectSlotsUnlimited : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CharacterGroups", "HaveDirectSlots", c => c.Boolean(nullable: false, defaultValue: true));
        }
        
        public override void Down()
        {
            DropColumn("dbo.CharacterGroups", "HaveDirectSlots");
        }
    }
}
