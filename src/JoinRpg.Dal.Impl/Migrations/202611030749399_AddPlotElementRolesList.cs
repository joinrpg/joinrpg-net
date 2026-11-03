namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class AddPlotElementRolesList : DbMigration
{
    // Nullable int без FK: сетки ролей удаляются жёстко, ссылка из вводной мягкая (ADR029).
    public override void Up()
    {
        AddColumn("dbo.PlotElements", "ProjectRolesListId", c => c.Int());
    }

    public override void Down()
    {
        DropColumn("dbo.PlotElements", "ProjectRolesListId");
    }
}
