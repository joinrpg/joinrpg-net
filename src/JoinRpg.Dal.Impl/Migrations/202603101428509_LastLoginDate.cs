namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class LastLoginDate : DbMigration
{
    public override void Up()
    {
        AddColumn("dbo.UserAuthDetails", "LastLoginDate", c => c.DateTimeOffset(precision: 7));
    }

    public override void Down()
    {
        DropColumn("dbo.UserAuthDetails", "LastLoginDate");
    }
}
