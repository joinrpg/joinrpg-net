namespace JoinRpg.Dal.Impl.Migrations
{
  using System;
  using System.Data.Entity.Migrations;

  public partial class Forums : DbMigration
  {
    public override void Up()
    {
      CreateTable(
          "dbo.ForumThreads",
          c => new
          {
            ForumThreadId = c.Int(nullable: false, identity: true),
            CharacterGroupId = c.Int(nullable: false),
            ProjectId = c.Int(nullable: false),
            Header = c.String(),
            CreatedAt = c.DateTime(nullable: false),
            ModifiedAt = c.DateTime(nullable: false),
            AuthorUserId = c.Int(nullable: false),
          })
        .PrimaryKey(t => t.ForumThreadId)
        .ForeignKey("dbo.Users", t => t.AuthorUserId, cascadeDelete: true)
        .ForeignKey("dbo.CharacterGroups", t => t.CharacterGroupId, cascadeDelete: true)
        .ForeignKey("dbo.Projects", t => t.ProjectId)
        .Index(t => t.CharacterGroupId)
        .Index(t => t.ProjectId)
        .Index(t => t.AuthorUserId);

      AddColumn("dbo.Comments", "ForumThreadId", c => c.Int());
      RenameColumn("dbo.Comments", "CreatedTime", "CreatedAt");
      CreateIndex("dbo.Comments", "ForumThreadId");
      AddForeignKey("dbo.Comments", "ForumThreadId", "dbo.ForumThreads", "ForumThreadId");
    }

    public override void Down()
    {
      RenameColumn("dbo.Comments", "CreatedAt", "CreatedTime");
      DropForeignKey("dbo.ForumThreads", "ProjectId", "dbo.Projects");
      DropForeignKey("dbo.Comments", "ForumThreadId", "dbo.ForumThreads");
      DropForeignKey("dbo.ForumThreads", "CharacterGroupId", "dbo.CharacterGroups");
      DropForeignKey("dbo.ForumThreads", "AuthorUserId", "dbo.Users");
      DropIndex("dbo.Comments", new[] {"ForumThreadId"});
      DropIndex("dbo.ForumThreads", new[] {"AuthorUserId"});
      DropIndex("dbo.ForumThreads", new[] {"ProjectId"});
      DropIndex("dbo.ForumThreads", new[] {"CharacterGroupId"});
      DropColumn("dbo.Comments", "ForumThreadId");
      DropTable("dbo.ForumThreads");
    }
  }
}