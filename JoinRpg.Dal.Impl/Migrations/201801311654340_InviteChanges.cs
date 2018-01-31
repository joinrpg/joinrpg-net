namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InviteChanges : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ProjectAccommodationTypes", "Description_Contents", c => c.String());
            DropColumn("dbo.ProjectAccommodationTypes", "Description");
        }
        
        public override void Down()
        {
            AddColumn("dbo.ProjectAccommodationTypes", "Description", c => c.String());
            DropColumn("dbo.ProjectAccommodationTypes", "Description_Contents");
        }
    }
}
