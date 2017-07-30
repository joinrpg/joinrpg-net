namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class Description : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CharacterGroups", "Description_Contents", c => c.String());
            AddColumn("dbo.Characters", "Description_Contents", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Characters", "Description_Contents");
            DropColumn("dbo.CharacterGroups", "Description_Contents");
        }
    }
}
