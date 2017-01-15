namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;

  public partial class Forums2 : DbMigration
  {
    public override void Up()
    {
      DropForeignKey("dbo.Comments", "ClaimId", "dbo.Claims");
      DropIndex("dbo.Comments", new[] {"ClaimId"});
      AlterColumn("dbo.Comments", "ClaimId", c => c.Int());
      CreateIndex("dbo.Comments", "ClaimId");
      AddForeignKey("dbo.Comments", "ClaimId", "dbo.Claims", "ClaimId");
    }

    public override void Down()
    {
      DropForeignKey("dbo.Comments", "ClaimId", "dbo.Claims");
      DropIndex("dbo.Comments", new[] {"ClaimId"});
      AlterColumn("dbo.Comments", "ClaimId", c => c.Int(nullable: false));
      CreateIndex("dbo.Comments", "ClaimId");
      AddForeignKey("dbo.Comments", "ClaimId", "dbo.Claims", "ClaimId", cascadeDelete: true);
    }
  }
}