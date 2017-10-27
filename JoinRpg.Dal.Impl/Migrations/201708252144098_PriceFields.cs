namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class PriceFields : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ProjectFields", "Price", c => c.Int(nullable: false, defaultValue: 0));
            AddColumn("dbo.ProjectFieldDropdownValues", "Price", c => c.Int(nullable: false, defaultValue: 0));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ProjectFieldDropdownValues", "Price");
            DropColumn("dbo.ProjectFields", "Price");
        }
    }
}
