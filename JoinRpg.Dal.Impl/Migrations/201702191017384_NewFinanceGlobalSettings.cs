namespace JoinRpg.Dal.Impl.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class NewFinanceGlobalSettings : DbMigration
    {
        public override void Up()
        {
            Sql(@"INSERT INTO dbo.ProjectDetails
(ProjectId)
SELECT P.ProjectId
FROM Projects P
LEFT JOIN ProjectDetails PD ON PD.ProjectId = P.ProjectId
WHERE PD.ProjectId IS NULL");
            AddColumn("dbo.ProjectDetails", "FinanceWarnOnOverPayment", c => c.Boolean(nullable: false, defaultValue: true));
        }

        public override void Down()
        {
            DropColumn("dbo.ProjectDetails", "FinanceWarnOnOverPayment");
        }
    }
}
