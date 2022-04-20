namespace JoinRpg.Dal.Impl.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class ResolveInviteToCode : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.AccommodationInvites", "ResolveDescription");
            AddColumn("dbo.AccommodationInvites", "ResolveDescription", c => c.Int(nullable: false));
        }

        public override void Down()
        {
            DropColumn("dbo.AccommodationInvites", "ResolveDescription");
            AddColumn("dbo.AccommodationInvites", "ResolveDescription", c => c.String());
        }
    }
}
