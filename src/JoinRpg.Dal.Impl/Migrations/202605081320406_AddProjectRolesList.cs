namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddProjectRolesList : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ProjectRolesLists",
                c => new
                    {
                        ProjectRolesListId = c.Int(nullable: false, identity: true),
                        ProjectId = c.Int(nullable: false),
                        Name = c.String(nullable: false),
                        CharacterGroupId = c.Int(),
                        PublicMode = c.Boolean(nullable: false),
                        FieldsImpl_ListIds = c.String(),
                        ContactsColumn = c.Int(nullable: false),
                        GroupsColumn = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ProjectRolesListId)
                .ForeignKey("dbo.CharacterGroups", t => t.CharacterGroupId)
                .ForeignKey("dbo.Projects", t => t.ProjectId, cascadeDelete: true)
                .Index(t => t.ProjectId)
                .Index(t => t.CharacterGroupId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ProjectRolesLists", "ProjectId", "dbo.Projects");
            DropForeignKey("dbo.ProjectRolesLists", "CharacterGroupId", "dbo.CharacterGroups");
            DropIndex("dbo.ProjectRolesLists", new[] { "CharacterGroupId" });
            DropIndex("dbo.ProjectRolesLists", new[] { "ProjectId" });
            DropTable("dbo.ProjectRolesLists");
        }
    }
}
