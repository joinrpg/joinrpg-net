namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class Invite_ResolveDescription : DbMigration
{
    public override void Up() => AddColumn("dbo.AccommodationInvites", "ResolveDescription", c => c.String());

    public override void Down() => DropColumn("dbo.AccommodationInvites", "ResolveDescription");
}
