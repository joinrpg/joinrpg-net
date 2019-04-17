namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SecurityStamp : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserAuthDetails", "AspNetSecurityStamp", c => c.String(nullable: false, defaultValueSql:"CAST(newid() AS varchar(100))"));
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserAuthDetails", "AspNetSecurityStamp");
        }
    }
}
