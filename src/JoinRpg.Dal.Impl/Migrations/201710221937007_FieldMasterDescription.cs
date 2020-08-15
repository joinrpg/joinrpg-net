namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class FieldMasterDescription : DbMigration
    {
        public override void Up() => AddColumn("dbo.ProjectFields", "MasterDescription_Contents", c => c.String());

        public override void Down() => DropColumn("dbo.ProjectFields", "MasterDescription_Contents");
    }
}
