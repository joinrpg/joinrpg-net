using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.DataModel
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global used by Entity Framework
    public class ProjectFeeSetting: IValidatableObject
    {
        public int ProjectFeeSettingId { get; set; }
        public int ProjectId { get; set; }
        public virtual Project Project { get; set; }
        public int Fee { get; set; }
        public DateTime StartDate { get; set; }

        // Used to initialize start date here to use it in dialog.
        // Tomorrow is used to make sure that wrong fee will not be applied
        // by the immediate subsequent payment confirmation.
        public static DateTime MinDate
            => DateTime.UtcNow.AddDays(1);

        #region Implementation of IValidatableObject

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Fee < 0)
                yield return
                    new ValidationResult("Fee should be positive", new List<string> {nameof(Fee)});
        }

        #endregion
    }
}
