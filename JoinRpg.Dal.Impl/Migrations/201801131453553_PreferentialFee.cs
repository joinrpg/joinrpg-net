namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class PreferentialFee : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Claims", "PreferentialFeeUser", c => c.Boolean(nullable: false, defaultValue: false));
            AddColumn("dbo.ProjectDetails", "PreferentialFeeEnabled", c => c.Boolean(nullable: false, defaultValue: false));
            AddColumn("dbo.ProjectDetails", "PreferentialFeeConditions_Contents", c => c.String());
            AddColumn("dbo.ProjectFeeSettings", "PreferentialFee", c => c.Int(nullable: true));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ProjectFeeSettings", "PreferentialFee");
            DropColumn("dbo.ProjectDetails", "PreferentialFeeConditions_Contents");
            DropColumn("dbo.ProjectDetails", "PreferentialFeeEnabled");
            DropColumn("dbo.Claims", "PreferentialFeeUser");
        }
    }
}
