namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class FieldProgrammaticValue : DbMigration
    {
        public override void Up() => AddColumn("dbo.ProjectFields", "ProgrammaticValue", c => c.String());

        public override void Down() => DropColumn("dbo.ProjectFields", "ProgrammaticValue");
    }
}
