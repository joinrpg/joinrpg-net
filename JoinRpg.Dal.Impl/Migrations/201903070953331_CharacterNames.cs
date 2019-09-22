namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CharacterNames : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.ProjectFields", "CharacterGroupId");

            AddColumn("dbo.ProjectDetails", "CharacterNameLegacyMode", c => c.Boolean(nullable: false));
            AddColumn("dbo.ProjectDetails", "FieldsOrdering", c => c.String());
            AddColumn("dbo.ProjectDetails", "CharacterDescription_ProjectFieldId", c => c.Int());
            AddColumn("dbo.ProjectDetails", "CharacterNameField_ProjectFieldId", c => c.Int());
            CreateIndex("dbo.ProjectDetails", "CharacterDescription_ProjectFieldId");
            CreateIndex("dbo.ProjectDetails", "CharacterNameField_ProjectFieldId");
            AddForeignKey("dbo.ProjectDetails", "CharacterDescription_ProjectFieldId", "dbo.ProjectFields", "ProjectFieldId");
            AddForeignKey("dbo.ProjectDetails", "CharacterNameField_ProjectFieldId", "dbo.ProjectFields", "ProjectFieldId");

            /*Sql(@"

INSERT INTO [dbo].[ProjectFields]
           ([FieldName]
           ,[FieldType]
           ,[IsPublic]
           ,[CanPlayerView]
           ,[CanPlayerEdit]
           ,[ProjectId]
           ,[IsActive]
           ,[WasEverUsed]
           ,[Description_Contents]
           ,[CharacterGroup_CharacterGroupId]
           ,[ValuesOrdering]
           ,[FieldBoundTo]
           ,[MandatoryStatus]
           ,[ValidForNpc]
           ,[AviableForImpl_ListIds]
           ,[IncludeInPrint]
           ,[ShowOnUnApprovedClaims]
           ,[Price]
           ,[MasterDescription_Contents]
           ,[ProgrammaticValue])
     SELECT 
           '$$$Description' -- <FieldName, nvarchar(max),>
           , 1 -- <FieldType, int,> = text
           ,1 -- <IsPublic, bit,>
           ,1 -- <CanPlayerView, bit,>
           ,0 -- CanPlayerEdit
           ,P.ProjectId
           ,1 -- IsActive
           ,1 -- WasEverUsed
           ,'' -- Description_Contents
           ,NULL -- CharacterGroup_CharacterGroupId
           ,'' -- ValuesOrdering
           ,0 -- <FieldBoundTo, int,> = character
           ,0 -- MandatoryStatus = optional
           ,1 -- ValidForNpc=true
           ,NULL -- AviableForImpl_ListIds = NULL
           ,1 -- IncludeInPrint
           ,1 -- ShowOnUnApprovedClaims
           ,0 -- Price
           ,'' -- MasterDescription_Contents
           ,'' -- ProgrammaticValue
    FROM Projects P


UPDATE ProjectDetails
SET
FieldsOrdering = CAST(PF.ProjectFieldId  AS VARCHAR) + ',' + ISNULL(P.ProjectFieldsOrdering, ''),
CharacterNameLegacyMode = CASE WHEN GenerateCharacterNamesFromPlayer = 1 THEN 0 ELSE 1 END,
CharacterDescription_ProjectFieldId = PF.ProjectFieldId
FROM ProjectDetails PD
INNER JOIN Projects P ON P.ProjectId = PD.ProjectId
INNER JOIN ProjectFields PF ON PF.ProjectId = P.ProjectId
            WHERE PF.FieldName LIKE '$$$Description'


UPDATE Characters
SET
 JsonData = JSON_MODIFY(ISNULL(JsonData, '{}'), '$.""' + CAST(PF.ProjectFieldId  AS VARCHAR) + '""', C.Description_Contents)

            FROM Characters C
                INNER JOIN Projects P ON P.ProjectId = C.ProjectId
            INNER JOIN ProjectFields PF ON PF.ProjectId = C.ProjectId
            WHERE PF.FieldName LIKE '$$$Description'

UPDATE ProjectFields
SET FieldName = 'Описание персонажа'
WHERE FieldName LIKE '$$$Description'

");*/
            DropColumn("dbo.Projects", "ProjectFieldsOrdering");
            DropColumn("dbo.ProjectDetails", "AllrpgId");
            DropColumn("dbo.ProjectDetails", "GenerateCharacterNamesFromPlayer");
            
        }
        
        public override void Down()
        {
            AddColumn("dbo.ProjectFields", "CharacterGroupId", c => c.Int(nullable: false));
            AddColumn("dbo.ProjectDetails", "GenerateCharacterNamesFromPlayer", c => c.Boolean(nullable: false));
            AddColumn("dbo.ProjectDetails", "AllrpgId", c => c.Int());
            AddColumn("dbo.Projects", "ProjectFieldsOrdering", c => c.String());
            DropForeignKey("dbo.ProjectDetails", "CharacterNameField_ProjectFieldId", "dbo.ProjectFields");
            DropForeignKey("dbo.ProjectDetails", "CharacterDescription_ProjectFieldId", "dbo.ProjectFields");
            DropIndex("dbo.ProjectDetails", new[] { "CharacterNameField_ProjectFieldId" });
            DropIndex("dbo.ProjectDetails", new[] { "CharacterDescription_ProjectFieldId" });
            DropColumn("dbo.ProjectDetails", "CharacterNameField_ProjectFieldId");
            DropColumn("dbo.ProjectDetails", "CharacterDescription_ProjectFieldId");
            DropColumn("dbo.ProjectDetails", "FieldsOrdering");
            DropColumn("dbo.ProjectDetails", "CharacterNameLegacyMode");
        }
    }
}
