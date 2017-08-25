namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class PriceFields : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ProjectFields", "Price", c => c.Int(false, false, 0));
            AddColumn("dbo.ProjectFieldDropdownValues", "Price", c => c.Int(false, false, 0));
        }

        public override void Down()
        {
            DropColumn("dbo.ProjectFields", "Price");
            DropColumn("dbo.ProjectFieldDropdownValues", "Price");
        }
  }
}
