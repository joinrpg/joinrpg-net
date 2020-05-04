namespace JoinRpg.Dal.Impl.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class RequiredFieldLabel : DbMigration
    {
        public override void Up()
        {
            Sql(@"UPDATE ProjectFieldDropdownValues 
          SET Label = 'ChangeMe!!!'
          WHERE Label IS NULL");
            //That's not happening in prod but better safe than sorry
            AlterColumn("dbo.ProjectFieldDropdownValues", "Label", c => c.String(nullable: false));
        }

        public override void Down()
        {
            AlterColumn("dbo.ProjectFieldDropdownValues", "Label", c => c.String());
        }
    }
}
