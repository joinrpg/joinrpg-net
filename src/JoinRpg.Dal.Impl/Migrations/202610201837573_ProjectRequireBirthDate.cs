namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class ProjectRequireBirthDate : DbMigration
{
    // 0 — MandatoryStatus.Optional: существующие проекты дату рождения не запрашивают.
    public override void Up() => AddColumn("dbo.ProjectDetails", "RequireBirthDate", c => c.Int(nullable: false, defaultValue: 0));

    public override void Down() => DropColumn("dbo.ProjectDetails", "RequireBirthDate");
}
