namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Invite_ResolveDescription : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ProjectAccommodationTypes", "Description", c => c.String());
            DropColumn("dbo.ProjectAccommodationTypes", "Description_Contents");
        }
        
        public override void Down()
        {
            AddColumn("dbo.ProjectAccommodationTypes", "Description_Contents", c => c.String());
            DropColumn("dbo.ProjectAccommodationTypes", "Description");
        }
    }
}
