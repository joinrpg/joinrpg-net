using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using Shouldly;
using Xunit;

namespace JoinRpg.Domain.Test
{
    public class ScheduleBuilderTests
    {
        [Fact]
        public void BuildSimpleSchedule()
        {
            var mock = new MockedProject();
            var scheduleField = mock.CreateField(new ProjectField { FieldType = ProjectFieldType.Dropdown });
            var firstSlot = mock.CreateFieldVariant(scheduleField, new ProjectFieldDropdownValue { Label = "10:00—11:00" });
            var secondSlot = mock.CreateFieldVariant(scheduleField, new ProjectFieldDropdownValue { Label = "11:00—12:00" });

            var roomField = mock.CreateField(new ProjectField { FieldType = ProjectFieldType.Dropdown });
            var firstRoom = mock.CreateFieldVariant(scheduleField, new ProjectFieldDropdownValue { Label = "Большой зал" });
            var secondRoom = mock.CreateFieldVariant(scheduleField, new ProjectFieldDropdownValue { Label = "Маленький зал" });

            var builder = new ScheduleBuilder(mock.Project, mock.Project.Characters);
            Should.NotThrow(() => builder.Build());
        }
    }
}
