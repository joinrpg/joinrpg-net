namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class FieldsMandatory : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ProjectFields", "MandatoryStatus", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ProjectFields", "MandatoryStatus");
        }
    }
}
