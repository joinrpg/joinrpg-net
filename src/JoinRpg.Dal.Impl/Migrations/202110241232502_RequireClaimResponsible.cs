namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class RequireClaimResponsible : DbMigration
    {
        public override void Up()
        {
            Sql(@"
UPDATE Claims
SET[ResponsibleMasterUserId] = ACL.UserId
FROM
Claims
LEFT JOIN ProjectAcls ACL ON Claims.ProjectId = ACL.ProjectId AND ACL.IsOwner = 1
WHERE[ResponsibleMasterUserId] is null");
            Sql("DROP INDEX IF EXISTS [nci_wi_Claims_DD27A24D517BFF9AFEC7C6C5C8FCCA84] ON [dbo].[Claims]");
            DropForeignKey("dbo.Claims", "ResponsibleMasterUserId", "dbo.Users");
            DropIndex("dbo.Claims", new[] { "ResponsibleMasterUserId" });
            AlterColumn("dbo.Claims", "ResponsibleMasterUserId", c => c.Int(nullable: false));
            CreateIndex("dbo.Claims", "ResponsibleMasterUserId");

            Sql(@"CREATE NONCLUSTERED INDEX [nci_wi_Claims_DD27A24D517BFF9AFEC7C6C5C8FCCA84] ON [dbo].[Claims]
(
	[ProjectId] ASC,
	[ClaimStatus] ASC
)
INCLUDE([CharacterGroupId],[CharacterId],[CommentDiscussionId],[CreateDate],[CurrentFee],[JsonData],[LastUpdateDateTime],[MasterAcceptedDate],[MasterDeclinedDate],[PlayerAcceptedDate],[PlayerDeclinedDate],[PlayerUserId],[ResponsibleMasterUserId]) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO");
            AddForeignKey("dbo.Claims", "ResponsibleMasterUserId", "dbo.Users", "UserId", cascadeDelete: true);
        }

        public override void Down()
        {
            DropForeignKey("dbo.Claims", "ResponsibleMasterUserId", "dbo.Users");
            DropIndex("dbo.Claims", new[] { "ResponsibleMasterUserId" });
            AlterColumn("dbo.Claims", "ResponsibleMasterUserId", c => c.Int());
            CreateIndex("dbo.Claims", "ResponsibleMasterUserId");
            AddForeignKey("dbo.Claims", "ResponsibleMasterUserId", "dbo.Users", "UserId");
        }
    }
}
