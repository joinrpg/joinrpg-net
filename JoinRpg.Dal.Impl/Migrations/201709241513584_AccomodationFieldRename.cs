namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AccomodationFieldRename : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ProjectDetails", "EnableAccomodation", c => c.Boolean(nullable: false));
            DropColumn("dbo.ProjectDetails", "AccomodationEnabled");
        }
        
        public override void Down()
        {
            AddColumn("dbo.ProjectDetails", "AccomodationEnabled", c => c.Boolean(nullable: false));
            DropColumn("dbo.ProjectDetails", "EnableAccomodation");
        }
    }
}
