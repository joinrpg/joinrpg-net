using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;

namespace JoinRpg.DataModel
{
    [ComplexType]
    public class ProjectScheduleSettings : IValidatableObject
    {
        [CanBeNull]
        public ProjectField TimeSlotField { get; set; }

        [CanBeNull]
        public ProjectField RoomField { get; set; }

        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            if (TimeSlotField == null && RoomField == null)
            {
                yield break;
            }

            if (TimeSlotField == null)
            {
                yield return new ValidationResult("Both fields should be set for schedules working properly", new[] { nameof(TimeSlotField) });
            }
            else
            {
                if (!TimeSlotField.FieldType.HasValuesList())
                {
                    yield return new ValidationResult("Field should be of type Dropdown / Multiselect for schedules working properly", new[] { nameof(TimeSlotField) });
                }
            }

            if (RoomField == null)
            {
                yield return new ValidationResult("Both fields should be set for schedules working properly", new[] { nameof(RoomField) });
            }
            else
            {
                if (!RoomField.FieldType.HasValuesList())
                {
                    yield return new ValidationResult("Field should be of type Dropdown / Multiselect for schedules working properly", new[] { nameof(RoomField) });
                }
            }
        }
    }
}
