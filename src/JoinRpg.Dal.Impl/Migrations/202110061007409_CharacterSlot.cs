namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class CharacterSlot : DbMigration
{
    public override void Up()
    {
        AddColumn("dbo.Characters", "CharacterType", c => c.Int(nullable: false, defaultValue: 0));
        AddColumn("dbo.Characters", "CharacterSlotLimit", c => c.Int());
        Sql("UPDATE dbo.Characters SET CharacterType = 1 WHERE IsAcceptingClaims = 0 ");
        AddColumn("dbo.Characters", "OriginalCharacterSlot_CharacterId", c => c.Int());
        CreateIndex("dbo.Characters", "OriginalCharacterSlot_CharacterId");
        AddForeignKey("dbo.Characters", "OriginalCharacterSlot_CharacterId", "dbo.Characters", "CharacterId");
    }

    public override void Down()
    {
        DropForeignKey("dbo.Characters", "OriginalCharacterSlot_CharacterId", "dbo.Characters");
        DropIndex("dbo.Characters", new[] { "OriginalCharacterSlot_CharacterId" });
        DropColumn("dbo.Characters", "OriginalCharacterSlot_CharacterId");
        DropColumn("dbo.Characters", "CharacterSlotLimit");
        DropColumn("dbo.Characters", "CharacterType");
    }
}
