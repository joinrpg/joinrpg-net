namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class CharacterGroupOrdering : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CharacterGroups", "ChildCharactersOrdering", c => c.String());
            AddColumn("dbo.CharacterGroups", "ChildGroupsOrdering", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.CharacterGroups", "ChildGroupsOrdering");
            DropColumn("dbo.CharacterGroups", "ChildCharactersOrdering");
        }
    }
}
