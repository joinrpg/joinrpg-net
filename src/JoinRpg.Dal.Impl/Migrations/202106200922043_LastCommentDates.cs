namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class LastCommentDates : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Claims", "LastMasterCommentAt", c => c.DateTimeOffset(precision: 7));
            AddColumn("dbo.Claims", "LastMasterCommentBy_Id", c => c.Int());
            AddColumn("dbo.Claims", "LastVisibleMasterCommentAt", c => c.DateTimeOffset(precision: 7));
            AddColumn("dbo.Claims", "LastVisibleMasterCommentBy_Id", c => c.Int());
            AddColumn("dbo.Claims", "LastPlayerCommentAt", c => c.DateTimeOffset(precision: 7));
            CreateIndex("dbo.Claims", "LastMasterCommentBy_Id");
            CreateIndex("dbo.Claims", "LastVisibleMasterCommentBy_Id");
            AddForeignKey("dbo.Claims", "LastMasterCommentBy_Id", "dbo.Users", "UserId");
            AddForeignKey("dbo.Claims", "LastVisibleMasterCommentBy_Id", "dbo.Users", "UserId");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Claims", "LastVisibleMasterCommentBy_Id", "dbo.Users");
            DropForeignKey("dbo.Claims", "LastMasterCommentBy_Id", "dbo.Users");
            DropIndex("dbo.Claims", new[] { "LastVisibleMasterCommentBy_Id" });
            DropIndex("dbo.Claims", new[] { "LastMasterCommentBy_Id" });
            DropColumn("dbo.Claims", "LastPlayerCommentAt");
            DropColumn("dbo.Claims", "LastVisibleMasterCommentBy_Id");
            DropColumn("dbo.Claims", "LastVisibleMasterCommentAt");
            DropColumn("dbo.Claims", "LastMasterCommentBy_Id");
            DropColumn("dbo.Claims", "LastMasterCommentAt");
        }
    }
}
