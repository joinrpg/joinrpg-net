namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class SaveAvatar : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.UserAvatars",
                c => new
                {
                    UserAvatarId = c.Int(nullable: false, identity: true),
                    UserId = c.Int(nullable: false),
                    AvatarSource = c.Int(nullable: false),
                    ProviderId = c.String(),
                    CachedUri = c.String(),
                    OriginalUri = c.String(),
                    IsActive = c.Boolean(nullable: false),
                })
                .PrimaryKey(t => t.UserAvatarId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId);

            AddColumn("dbo.Users", "SelectedAvatarId", c => c.Int());
            CreateIndex("dbo.Users", "SelectedAvatarId");
            AddForeignKey("dbo.Users", "SelectedAvatarId", "dbo.UserAvatars", "UserAvatarId");
        }

        public override void Down()
        {
            DropForeignKey("dbo.Users", "SelectedAvatarId", "dbo.UserAvatars");
            DropForeignKey("dbo.UserAvatars", "UserId", "dbo.Users");
            DropIndex("dbo.UserAvatars", new[] { "UserId" });
            DropIndex("dbo.Users", new[] { "SelectedAvatarId" });
            DropColumn("dbo.Users", "SelectedAvatarId");
            DropTable("dbo.UserAvatars");
        }
    }
}
