namespace JoinRpg.Dal.Impl.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class InviteUpdate : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AccommodationInvites", "IsGroupInvite", c => c.Boolean(nullable: false));
            AddColumn("dbo.AccommodationInvites", "ResolveDescription", c => c.String());
        }

        public override void Down()
        {
            DropColumn("dbo.AccommodationInvites", "ResolveDescription");
            DropColumn("dbo.AccommodationInvites", "IsGroupInvite");
        }
    }
}
