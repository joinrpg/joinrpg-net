namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;

  public partial class CharacterAndGroupsCreateUpdateDates : DbMigration
  {
    public override void Up()
    {
      //Assign owners where they are lost
      Sql(@"UPDATE ProjectAcls
          SET
            IsOwner = 1
          FROM ProjectAcls P
            INNER JOIN(
            SELECT P.ProjectId, MIN(Acl.UserId) AS NewOwnerId
          FROM Projects P
            LEFT JOIN ProjectAcls CR ON CR.ProjectId = P.ProjectId AND Cr.IsOwner = 1
          LEFT JOIN ProjectAcls Acl ON Acl.ProjectId = P.ProjectId
          WHERE CR.UserId IS NULL
            GROUP BY P.ProjectId) T ON T.ProjectId = P.ProjectId AND T.NewOwnerId = P.UserId");

      AddColumn("dbo.Characters", "CreatedAt", c => c.DateTime(nullable: true));
      AddColumn("dbo.Characters", "CreatedById", c => c.Int(nullable: true));
      AddColumn("dbo.Characters", "UpdatedAt", c => c.DateTime(nullable: true));
      AddColumn("dbo.Characters", "UpdatedById", c => c.Int(nullable: true));

      Sql(@"UPDATE Characters
          SET
            CreatedAt = P.CreatedDate,
            CreatedById = CREATOR.UserId,
            UpdatedAt = P.CharacterTreeModifiedAt,
            UpdatedById = CREATOR.UserId
          FROM Characters C
            INNER JOIN Projects P ON C.ProjectId = P.ProjectId
          INNER JOIN ProjectAcls CREATOR ON CREATOR.ProjectId = P.ProjectId AND CREATOR.IsOwner = 1
");

      AlterColumn("dbo.Characters", "CreatedAt", c => c.DateTime(nullable: false));
      AlterColumn("dbo.Characters", "CreatedById", c => c.Int(nullable: false));
      AlterColumn("dbo.Characters", "UpdatedAt", c => c.DateTime(nullable: false));
      AlterColumn("dbo.Characters", "UpdatedById", c => c.Int(nullable: false));

      AddColumn("dbo.CharacterGroups", "CreatedAt", c => c.DateTime(nullable: true));
      AddColumn("dbo.CharacterGroups", "CreatedById", c => c.Int(nullable: true));
      AddColumn("dbo.CharacterGroups", "UpdatedAt", c => c.DateTime(nullable: true));
      AddColumn("dbo.CharacterGroups", "UpdatedById", c => c.Int(nullable: true));


      Sql(@"UPDATE CharacterGroups
          SET
            CreatedAt = P.CreatedDate,
            CreatedById = CREATOR.UserId,
            UpdatedAt = P.CharacterTreeModifiedAt,
            UpdatedById = CREATOR.UserId
          FROM CharacterGroups C
            INNER JOIN Projects P ON C.ProjectId = P.ProjectId
          INNER JOIN ProjectAcls CREATOR ON CREATOR.ProjectId = P.ProjectId AND CREATOR.IsOwner = 1");

      AlterColumn("dbo.CharacterGroups", "CreatedAt", c => c.DateTime(nullable: false));
      AlterColumn("dbo.CharacterGroups", "CreatedById", c => c.Int(nullable: false));
      AlterColumn("dbo.CharacterGroups", "UpdatedAt", c => c.DateTime(nullable: false));
      AlterColumn("dbo.CharacterGroups", "UpdatedById", c => c.Int(nullable: false));

      CreateIndex("dbo.Characters", "CreatedById");
      CreateIndex("dbo.Characters", "UpdatedById");
      CreateIndex("dbo.CharacterGroups", "CreatedById");
      CreateIndex("dbo.CharacterGroups", "UpdatedById");
      AddForeignKey("dbo.CharacterGroups", "CreatedById", "dbo.Users", "UserId");
      AddForeignKey("dbo.CharacterGroups", "UpdatedById", "dbo.Users", "UserId");
      AddForeignKey("dbo.Characters", "CreatedById", "dbo.Users", "UserId");
      AddForeignKey("dbo.Characters", "UpdatedById", "dbo.Users", "UserId");
    }

    public override void Down()
    {
      DropForeignKey("dbo.Characters", "UpdatedById", "dbo.Users");
      DropForeignKey("dbo.Characters", "CreatedById", "dbo.Users");
      DropForeignKey("dbo.CharacterGroups", "UpdatedById", "dbo.Users");
      DropForeignKey("dbo.CharacterGroups", "CreatedById", "dbo.Users");
      DropIndex("dbo.CharacterGroups", new[] {"UpdatedById"});
      DropIndex("dbo.CharacterGroups", new[] {"CreatedById"});
      DropIndex("dbo.Characters", new[] {"UpdatedById"});
      DropIndex("dbo.Characters", new[] {"CreatedById"});
      DropColumn("dbo.CharacterGroups", "UpdatedById");
      DropColumn("dbo.CharacterGroups", "UpdatedAt");
      DropColumn("dbo.CharacterGroups", "CreatedById");
      DropColumn("dbo.CharacterGroups", "CreatedAt");
      DropColumn("dbo.Characters", "UpdatedById");
      DropColumn("dbo.Characters", "UpdatedAt");
      DropColumn("dbo.Characters", "CreatedById");
      DropColumn("dbo.Characters", "CreatedAt");
    }
  }
}
