namespace JoinRpg.Dal.Impl.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AdministratorFlag : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserAuthDetails", "IsAdmin", c => c.Boolean(nullable: false, defaultValue: false));
        }

        public override void Down()
        {
            DropColumn("dbo.UserAuthDetails", "IsAdmin");
        }
    }
}
