namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class RemoveGroupClaims : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Claims", "CharacterGroupId", "dbo.CharacterGroups");
            DropIndex("dbo.Claims", new[] { "CharacterId" });
            DropIndex("dbo.Claims", new[] { "CharacterGroupId" });
            AlterColumn("dbo.Claims", "CharacterId", c => c.Int(nullable: false));
            CreateIndex("dbo.Claims", "CharacterId");
            DropColumn("dbo.Claims", "CharacterGroupId");
            DropColumn("dbo.CharacterGroups", "AvaiableDirectSlots");
            DropColumn("dbo.CharacterGroups", "HaveDirectSlots");
        }

        public override void Down()
        {
            AddColumn("dbo.CharacterGroups", "HaveDirectSlots", c => c.Boolean(nullable: false));
            AddColumn("dbo.CharacterGroups", "AvaiableDirectSlots", c => c.Int(nullable: false));
            AddColumn("dbo.Claims", "CharacterGroupId", c => c.Int());
            DropIndex("dbo.Claims", new[] { "CharacterId" });
            AlterColumn("dbo.Claims", "CharacterId", c => c.Int());
            CreateIndex("dbo.Claims", "CharacterGroupId");
            CreateIndex("dbo.Claims", "CharacterId");
            AddForeignKey("dbo.Claims", "CharacterGroupId", "dbo.CharacterGroups", "CharacterGroupId");
        }
    }
}
