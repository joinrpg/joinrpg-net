namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class SetkaTree : DbMigration
{
    public override void Up()
    {
        // Вместо сгенерированного AddColumn+DropColumn переименовываем колонку с сохранением
        // данных: значения enum совпадают с bit (false=0=None, true=1=Sections).
        RenameColumn("dbo.ProjectRolesLists", "ShowCharacterGroups", "GroupsViewMode");
        AlterColumn("dbo.ProjectRolesLists", "GroupsViewMode", c => c.Int(nullable: false));
    }

    public override void Down()
    {
        AlterColumn("dbo.ProjectRolesLists", "GroupsViewMode", c => c.Boolean(nullable: false));
        RenameColumn("dbo.ProjectRolesLists", "GroupsViewMode", "ShowCharacterGroups");
    }
}
