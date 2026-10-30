namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class AddAccommodationInvitesEnabled : DbMigration
{
    // true: существующие проекты сохраняют поведение — игроки могут приглашать друг друга.
    public override void Up() => AddColumn("dbo.ProjectDetails", "AccommodationInvitesEnabled", c => c.Boolean(nullable: false, defaultValue: true));

    public override void Down() => DropColumn("dbo.ProjectDetails", "AccommodationInvitesEnabled");
}
