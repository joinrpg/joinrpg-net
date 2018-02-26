namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class invitationmerged : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AccommodationInvites", "ResolveDescription", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AccommodationInvites", "ResolveDescription");
        }
    }
}
