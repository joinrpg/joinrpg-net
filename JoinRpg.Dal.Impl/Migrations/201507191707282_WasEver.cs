namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class WasEver : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ProjectCharacterFields", "WasEverUsed", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ProjectCharacterFields", "WasEverUsed");
        }
    }
}
