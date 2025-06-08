namespace JoinRpg.Dal.Impl.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class ActiveStatusForKogdaIgra : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.KogdaIgraGames", "Active", c => c.Boolean(nullable: false, defaultValue: true));
        }

        public override void Down()
        {
            DropColumn("dbo.KogdaIgraGames", "Active");
        }
    }
}
