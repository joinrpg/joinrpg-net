namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class FixDropDownvaluesLinks : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.ProjectFieldDropdownValues", "CharacterGroupId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.ProjectFieldDropdownValues", "CharacterGroupId", c => c.Int(nullable: false));
        }
    }
}
