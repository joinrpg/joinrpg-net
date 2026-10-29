namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class RolesListPlayerAvatar : DbMigration
{
    // 0 — PlayerAvatarMode.None: у существующих сеток аватарок нет.
    public override void Up() => AddColumn("dbo.ProjectRolesLists", "PlayerAvatar", c => c.Int(nullable: false, defaultValue: 0));

    public override void Down() => DropColumn("dbo.ProjectRolesLists", "PlayerAvatar");
}
