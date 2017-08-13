namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class DefaultPaymentType : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PaymentTypes", "IsDefault", c => c.Boolean(nullable: false, defaultValue: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.PaymentTypes", "IsDefault");
        }
    }
}
