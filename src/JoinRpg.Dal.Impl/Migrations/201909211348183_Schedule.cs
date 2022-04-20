namespace JoinRpg.Dal.Impl.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class Schedule : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ProjectScheduleSettings",
                c => new
                {
                    ProjectId = c.Int(nullable: false),
                    RoomField_ProjectFieldId = c.Int(),
                    TimeSlotField_ProjectFieldId = c.Int(),
                })
                .PrimaryKey(t => t.ProjectId)
                .ForeignKey("dbo.ProjectFields", t => t.RoomField_ProjectFieldId)
                .ForeignKey("dbo.ProjectFields", t => t.TimeSlotField_ProjectFieldId)
                .ForeignKey("dbo.ProjectDetails", t => t.ProjectId)
                .Index(t => t.ProjectId)
                .Index(t => t.RoomField_ProjectFieldId)
                .Index(t => t.TimeSlotField_ProjectFieldId);

        }

        public override void Down()
        {
            DropForeignKey("dbo.ProjectScheduleSettings", "ProjectId", "dbo.ProjectDetails");
            DropForeignKey("dbo.ProjectScheduleSettings", "TimeSlotField_ProjectFieldId", "dbo.ProjectFields");
            DropForeignKey("dbo.ProjectScheduleSettings", "RoomField_ProjectFieldId", "dbo.ProjectFields");
            DropIndex("dbo.ProjectScheduleSettings", new[] { "TimeSlotField_ProjectFieldId" });
            DropIndex("dbo.ProjectScheduleSettings", new[] { "RoomField_ProjectFieldId" });
            DropIndex("dbo.ProjectScheduleSettings", new[] { "ProjectId" });
            DropTable("dbo.ProjectScheduleSettings");
        }
    }
}
