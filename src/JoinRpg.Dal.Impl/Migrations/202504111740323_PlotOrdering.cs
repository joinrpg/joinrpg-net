namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class PlotOrdering : DbMigration
{
    public override void Up()
    {
        DropForeignKey("dbo.PlotFolderCharacterGroups", "PlotFolder_PlotFolderId", "dbo.PlotFolders");
        DropForeignKey("dbo.PlotFolderCharacterGroups", "CharacterGroup_CharacterGroupId", "dbo.CharacterGroups");
        DropIndex("dbo.PlotFolderCharacterGroups", new[] { "PlotFolder_PlotFolderId" });
        DropIndex("dbo.PlotFolderCharacterGroups", new[] { "CharacterGroup_CharacterGroupId" });
        AddColumn("dbo.PlotFolders", "ElementsOrdering", c => c.String());
        AddColumn("dbo.ProjectDetails", "PlotFoldersOrdering", c => c.String());
        DropTable("dbo.PlotFolderCharacterGroups");
    }

    public override void Down()
    {
        CreateTable(
            "dbo.PlotFolderCharacterGroups",
            c => new
            {
                PlotFolder_PlotFolderId = c.Int(nullable: false),
                CharacterGroup_CharacterGroupId = c.Int(nullable: false),
            })
            .PrimaryKey(t => new { t.PlotFolder_PlotFolderId, t.CharacterGroup_CharacterGroupId });

        DropColumn("dbo.ProjectDetails", "PlotFoldersOrdering");
        DropColumn("dbo.PlotFolders", "ElementsOrdering");
        CreateIndex("dbo.PlotFolderCharacterGroups", "CharacterGroup_CharacterGroupId");
        CreateIndex("dbo.PlotFolderCharacterGroups", "PlotFolder_PlotFolderId");
        AddForeignKey("dbo.PlotFolderCharacterGroups", "CharacterGroup_CharacterGroupId", "dbo.CharacterGroups", "CharacterGroupId", cascadeDelete: true);
        AddForeignKey("dbo.PlotFolderCharacterGroups", "PlotFolder_PlotFolderId", "dbo.PlotFolders", "PlotFolderId", cascadeDelete: true);
    }
}
