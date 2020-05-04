namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class AccomodationSubscribe : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserSubscriptions", "AccommodationChange", c => c.Boolean(nullable: false, defaultValue: true));
        }

        public override void Down()
        {
            DropColumn("dbo.UserSubscriptions", "AccommodationChange");
        }
    }
}
