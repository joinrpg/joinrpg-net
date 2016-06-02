namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CharacterTreeModifiedTime : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Projects", "CharacterTreeModifiedAt", c => c.DateTime(nullable: false, defaultValue: DateTime.UtcNow));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Projects", "CharacterTreeModifiedAt");
        }
    }
}
