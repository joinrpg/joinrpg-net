namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class RoomDescription : DbMigration
    {
        public override void Up()
        {
            RenameColumn("dbo.ProjectAccommodationTypes", "Description", "Description_Contents");
        }

        public override void Down()
        {
            RenameColumn("dbo.ProjectAccommodationTypes", "Description_Contents", "Description");
        }
    }
}
