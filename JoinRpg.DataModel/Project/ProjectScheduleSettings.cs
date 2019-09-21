using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;

namespace JoinRpg.DataModel
{
    public class ProjectScheduleSettings : IValidatableObject
    {
        [Key] public int ProjectId { get; set; }

        [NotNull]
        [Required]
        public virtual ProjectField TimeSlotField { get; set; }

        [NotNull]
        [Required]
        public virtual ProjectField RoomField { get; set; }

        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
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
