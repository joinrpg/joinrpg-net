namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class NewParentGroups : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.CharacterGroupCharacterGroups", "CharacterGroup_CharacterGroupId", "dbo.CharacterGroups");
            DropForeignKey("dbo.CharacterGroupCharacterGroups", "CharacterGroup_CharacterGroupId1", "dbo.CharacterGroups");
            DropForeignKey("dbo.ProjectFieldCharacterGroups", "ProjectField_ProjectFieldId", "dbo.ProjectFields");
            DropForeignKey("dbo.ProjectFieldCharacterGroups", "CharacterGroup_CharacterGroupId", "dbo.CharacterGroups");
            DropForeignKey("dbo.CharacterCharacterGroups", "Character_CharacterId", "dbo.Characters");
            DropForeignKey("dbo.CharacterCharacterGroups", "CharacterGroup_CharacterGroupId", "dbo.CharacterGroups");
            DropIndex("dbo.CharacterGroupCharacterGroups", new[] { "CharacterGroup_CharacterGroupId" });
            DropIndex("dbo.CharacterGroupCharacterGroups", new[] { "CharacterGroup_CharacterGroupId1" });
            DropIndex("dbo.ProjectFieldCharacterGroups", new[] { "ProjectField_ProjectFieldId" });
            DropIndex("dbo.ProjectFieldCharacterGroups", new[] { "CharacterGroup_CharacterGroupId" });
            DropIndex("dbo.CharacterCharacterGroups", new[] { "Character_CharacterId" });
            DropIndex("dbo.CharacterCharacterGroups", new[] { "CharacterGroup_CharacterGroupId" });
            AddColumn("dbo.Characters", "ParentGroupsImpl_ListIds", c => c.String());
            AddColumn("dbo.CharacterGroups", "ParentGroupsImpl_ListIds", c => c.String());
            AddColumn("dbo.ProjectFields", "AviableForImpl_ListIds", c => c.String());
          Sql(@"UPDATE CharacterGroups 
SET ParentGroupsImpl_ListIds = Links.Parents
FROM CharacterGroups T
LEFT JOIN(
 SELECT CharacterGroup_CharacterGroupId, STUFF((SELECT N', ' + CONVERT(nvarchar, c2.CharacterGroup_CharacterGroupId1)
  FROM dbo.CharacterGroupCharacterGroups AS c2
   WHERE c2.CharacterGroup_CharacterGroupId = c.CharacterGroup_CharacterGroupId
   ORDER BY CharacterGroup_CharacterGroupId1
   FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 2, N'') As Parents
FROM CharacterGroupCharacterGroups c
GROUP BY CharacterGroup_CharacterGroupId) Links ON Links.CharacterGroup_CharacterGroupId = T.CharacterGroupId

UPDATE ProjectFields
SET AviableForImpl_ListIds = Links.Parents
FROM ProjectFields T
LEFT JOIN(
 SELECT c.ProjectField_ProjectFieldId, STUFF((SELECT N', ' + CONVERT(nvarchar, c2.CharacterGroup_CharacterGroupId)
  FROM dbo.ProjectFieldCharacterGroups AS c2
   WHERE c2.ProjectField_ProjectFieldId = c.ProjectField_ProjectFieldId
   ORDER BY CharacterGroup_CharacterGroupId
   FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 2, N'') As Parents
FROM ProjectFieldCharacterGroups c
GROUP BY ProjectField_ProjectFieldId) Links ON Links.ProjectField_ProjectFieldId = T.ProjectFieldId

UPDATE Characters
SET ParentGroupsImpl_ListIds = Links.Parents
FROM Characters T
LEFT JOIN(
 SELECT c.Character_CharacterId, STUFF((SELECT N', ' + CONVERT(nvarchar, c2.CharacterGroup_CharacterGroupId)
  FROM dbo.CharacterCharacterGroups AS c2
   WHERE c2.Character_CharacterId = c.Character_CharacterId
   ORDER BY CharacterGroup_CharacterGroupId
   FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 2, N'') As Parents
FROM CharacterCharacterGroups c
GROUP BY Character_CharacterId) Links ON Links.Character_CharacterId = T.CharacterId");
          DropTable("dbo.CharacterGroupCharacterGroups");
          DropTable("dbo.ProjectFieldCharacterGroups");
          DropTable("dbo.CharacterCharacterGroups");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.CharacterCharacterGroups",
                c => new
                    {
                        Character_CharacterId = c.Int(nullable: false),
                        CharacterGroup_CharacterGroupId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Character_CharacterId, t.CharacterGroup_CharacterGroupId });
            
            CreateTable(
                "dbo.ProjectFieldCharacterGroups",
                c => new
                    {
                        ProjectField_ProjectFieldId = c.Int(nullable: false),
                        CharacterGroup_CharacterGroupId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.ProjectField_ProjectFieldId, t.CharacterGroup_CharacterGroupId });
            
            CreateTable(
                "dbo.CharacterGroupCharacterGroups",
                c => new
                    {
                        CharacterGroup_CharacterGroupId = c.Int(nullable: false),
                        CharacterGroup_CharacterGroupId1 = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.CharacterGroup_CharacterGroupId, t.CharacterGroup_CharacterGroupId1 });
            
            DropColumn("dbo.ProjectFields", "AviableForImpl_ListIds");
            DropColumn("dbo.CharacterGroups", "ParentGroupsImpl_ListIds");
            DropColumn("dbo.Characters", "ParentGroupsImpl_ListIds");
            CreateIndex("dbo.CharacterCharacterGroups", "CharacterGroup_CharacterGroupId");
            CreateIndex("dbo.CharacterCharacterGroups", "Character_CharacterId");
            CreateIndex("dbo.ProjectFieldCharacterGroups", "CharacterGroup_CharacterGroupId");
            CreateIndex("dbo.ProjectFieldCharacterGroups", "ProjectField_ProjectFieldId");
            CreateIndex("dbo.CharacterGroupCharacterGroups", "CharacterGroup_CharacterGroupId1");
            CreateIndex("dbo.CharacterGroupCharacterGroups", "CharacterGroup_CharacterGroupId");
            AddForeignKey("dbo.CharacterCharacterGroups", "CharacterGroup_CharacterGroupId", "dbo.CharacterGroups", "CharacterGroupId", cascadeDelete: true);
            AddForeignKey("dbo.CharacterCharacterGroups", "Character_CharacterId", "dbo.Characters", "CharacterId", cascadeDelete: true);
            AddForeignKey("dbo.ProjectFieldCharacterGroups", "CharacterGroup_CharacterGroupId", "dbo.CharacterGroups", "CharacterGroupId", cascadeDelete: true);
            AddForeignKey("dbo.ProjectFieldCharacterGroups", "ProjectField_ProjectFieldId", "dbo.ProjectFields", "ProjectFieldId", cascadeDelete: true);
            AddForeignKey("dbo.CharacterGroupCharacterGroups", "CharacterGroup_CharacterGroupId1", "dbo.CharacterGroups", "CharacterGroupId");
            AddForeignKey("dbo.CharacterGroupCharacterGroups", "CharacterGroup_CharacterGroupId", "dbo.CharacterGroups", "CharacterGroupId");
        }
    }
}
