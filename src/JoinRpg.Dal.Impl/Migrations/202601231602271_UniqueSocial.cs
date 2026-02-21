namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class UniqueSocial : DbMigration
{
    public override void Up()
    {
        //  Удаляем все дубликаты (оставляем более новую запись)
        Sql(@"
WITH DuplicateCTE AS
(
    SELECT 
        UserExternalLoginId,
        UserId,
        Provider,
        ROW_NUMBER() OVER (
            PARTITION BY UserId, Provider 
            ORDER BY UserExternalLoginId DESC
        ) AS rn
    FROM dbo.UserExternalLogins
)

DELETE FROM DuplicateCTE
WHERE rn > 1
");
        DropIndex("dbo.UserExternalLogins", new[] { "UserId" });
        AlterColumn("dbo.UserExternalLogins", "Provider", c => c.String(maxLength: 450));
        CreateIndex("dbo.UserExternalLogins", new[] { "UserId", "Provider" }, unique: true, name: "IX_UserExternalLogin_UserId_Provider");
    }

    public override void Down()
    {
        DropIndex("dbo.UserExternalLogins", "IX_UserExternalLogin_UserId_Provider");
        AlterColumn("dbo.UserExternalLogins", "Provider", c => c.String());
        CreateIndex("dbo.UserExternalLogins", "UserId");
    }
}
