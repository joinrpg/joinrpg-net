namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AvailField : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CharacterGroups", "AvaiableDirectSlots", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.CharacterGroups", "AvaiableDirectSlots");
        }
    }
}
