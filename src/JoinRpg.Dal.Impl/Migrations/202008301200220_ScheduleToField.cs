namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ScheduleToField : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.ProjectScheduleSettings", "RoomField_ProjectFieldId", "dbo.ProjectFields");
            DropForeignKey("dbo.ProjectScheduleSettings", "TimeSlotField_ProjectFieldId", "dbo.ProjectFields");
            DropForeignKey("dbo.ProjectScheduleSettings", "ProjectId", "dbo.ProjectDetails");
            DropIndex("dbo.ProjectScheduleSettings", new[] { "ProjectId" });
            DropIndex("dbo.ProjectScheduleSettings", new[] { "RoomField_ProjectFieldId" });
            DropIndex("dbo.ProjectScheduleSettings", new[] { "TimeSlotField_ProjectFieldId" });
            AddColumn("dbo.ProjectDetails", "ScheduleEnabled", c => c.Boolean(nullable: false, defaultValue: false));

            Sql(@"
UPDATE dbo.ProjectDetails
SET ScheduleEnabled = 1
FROM dbo.ProjectDetails PD
INNER JOIN dbo.ProjectScheduleSettings PSS ON PSS.ProjectId = PD.ProjectId");

            Sql(@"
UPDATE dbo.ProjectFields
SET FieldType = 8
FROM dbo.ProjectFields F
INNER JOIN dbo.ProjectScheduleSettings PSS ON PSS.RoomField_ProjectFieldId = F.ProjectFieldId");

            Sql(@"
UPDATE dbo.ProjectFields
SET FieldType = 9
FROM dbo.ProjectFields F
INNER JOIN dbo.ProjectScheduleSettings PSS ON PSS.TimeSlotField_ProjectFieldId = F.ProjectFieldId");
            DropTable("dbo.ProjectScheduleSettings");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.ProjectScheduleSettings",
                c => new
                    {
                        ProjectId = c.Int(nullable: false),
                        RoomField_ProjectFieldId = c.Int(),
                        TimeSlotField_ProjectFieldId = c.Int(),
                    })
                .PrimaryKey(t => t.ProjectId);
            
            DropColumn("dbo.ProjectDetails", "ScheduleEnabled");
            CreateIndex("dbo.ProjectScheduleSettings", "TimeSlotField_ProjectFieldId");
            CreateIndex("dbo.ProjectScheduleSettings", "RoomField_ProjectFieldId");
            CreateIndex("dbo.ProjectScheduleSettings", "ProjectId");
            AddForeignKey("dbo.ProjectScheduleSettings", "ProjectId", "dbo.ProjectDetails", "ProjectId");
            AddForeignKey("dbo.ProjectScheduleSettings", "TimeSlotField_ProjectFieldId", "dbo.ProjectFields", "ProjectFieldId");
            AddForeignKey("dbo.ProjectScheduleSettings", "RoomField_ProjectFieldId", "dbo.ProjectFields", "ProjectFieldId");
        }
    }
}
