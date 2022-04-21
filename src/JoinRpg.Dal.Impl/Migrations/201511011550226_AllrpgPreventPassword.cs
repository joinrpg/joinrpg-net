namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class AllrpgPreventPassword : DbMigration
{
    public override void Up()
    {
        AddColumn("dbo.AllrpgUserDetails", "PreventAllrpgPassword",
          c => c.Boolean(nullable: false, defaultValue: true));
    }

    public override void Down() => DropColumn("dbo.AllrpgUserDetails", "PreventAllrpgPassword");
}
