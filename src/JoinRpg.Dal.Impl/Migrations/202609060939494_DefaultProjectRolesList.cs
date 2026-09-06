namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class DefaultProjectRolesList : DbMigration
{
    public override void Up()
    {
        AddColumn("dbo.ProjectDetails", "DefaultProjectRolesListId", c => c.Int());
        CreateIndex("dbo.ProjectDetails", "DefaultProjectRolesListId");
        AddForeignKey("dbo.ProjectDetails", "DefaultProjectRolesListId", "dbo.ProjectRolesLists", "ProjectRolesListId");

        // Старейшая (с минимальным Id) сетка ролей каждого проекта становится сеткой по умолчанию.
        Sql(@"
                UPDATE pd SET pd.DefaultProjectRolesListId =
                    (SELECT MIN(prl.ProjectRolesListId) FROM dbo.ProjectRolesLists prl WHERE prl.ProjectId = pd.ProjectId)
                FROM dbo.ProjectDetails pd
                WHERE EXISTS (SELECT 1 FROM dbo.ProjectRolesLists prl WHERE prl.ProjectId = pd.ProjectId)");
    }

    public override void Down()
    {
        DropForeignKey("dbo.ProjectDetails", "DefaultProjectRolesListId", "dbo.ProjectRolesLists");
        DropIndex("dbo.ProjectDetails", new[] { "DefaultProjectRolesListId" });
        DropColumn("dbo.ProjectDetails", "DefaultProjectRolesListId");
    }
}
