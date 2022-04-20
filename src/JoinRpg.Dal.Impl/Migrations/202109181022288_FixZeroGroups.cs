namespace JoinRpg.Dal.Impl.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class FixZeroGroups : DbMigration
    {
        public override void Up()
        {
            Sql(@"
UPDATE VariantGroup 

SET ParentGroupsImpl_ListIds = '' + FieldGroup.CharacterGroupId

FROM CharacterGroups VariantGroup
  LEFT JOIN ProjectFieldDropdownValues Variant ON Variant.CharacterGroup_CharacterGroupId = VariantGroup.CharacterGroupId
  LEFT JOIN ProjectFields Field ON Field.ProjectFieldId = Variant.ProjectFieldId
  LEFT JOIN CharacterGroups FieldGroup ON Field.CharacterGroup_CharacterGroupId = FieldGroup.CharacterGroupId
  WHERE VariantGroup.ParentGroupsImpl_ListIds LIKE '0'
");
        }

        public override void Down()
        {
        }
    }
}
