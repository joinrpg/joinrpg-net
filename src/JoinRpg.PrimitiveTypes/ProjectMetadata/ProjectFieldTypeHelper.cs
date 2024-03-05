namespace JoinRpg.PrimitiveTypes.ProjectMetadata;

public static class ProjectFieldTypeHelper
{
    /// <summary>
    /// Returns true if field supports price calculations
    /// </summary>
    public static bool SupportsPricing(this ProjectFieldType self)
    {
        return self switch
        {
            ProjectFieldType.Dropdown or ProjectFieldType.MultiSelect or ProjectFieldType.Checkbox or ProjectFieldType.Number => true,
            ProjectFieldType.String or ProjectFieldType.Text or ProjectFieldType.Header or ProjectFieldType.Login
                or ProjectFieldType.ScheduleRoomField or ProjectFieldType.ScheduleTimeSlotField or ProjectFieldType.PinCode
                or ProjectFieldType.Uri
                => false,
            _ => throw new ArgumentException(self.ToString(), nameof(self)),
        };
    }

    /// <summary>
    /// Returns true if price could be entered for field, not for it's values
    /// </summary>
    public static bool SupportsPricingOnField(this ProjectFieldType self)
    {
        if (self.HasValuesList())
        {
            return false;
        }

        return self switch
        {
            ProjectFieldType.Checkbox or ProjectFieldType.Number => true,

            ProjectFieldType.String or ProjectFieldType.Text or ProjectFieldType.Header or ProjectFieldType.Login
            or ProjectFieldType.PinCode or ProjectFieldType.Uri
                => false,
            _ => throw new ArgumentException(self.ToString(), nameof(self)),
        };
    }

    /// <summary>
    /// Returns true if field has predefined values
    /// </summary>
    public static bool HasValuesList(this ProjectFieldType self)
    {
        return self switch
        {
            ProjectFieldType.Dropdown => true,
            ProjectFieldType.MultiSelect => true,
            ProjectFieldType.ScheduleRoomField => true,
            ProjectFieldType.ScheduleTimeSlotField => true,
            ProjectFieldType.String => false,
            ProjectFieldType.Text => false,
            ProjectFieldType.Checkbox => false,
            ProjectFieldType.Header => false,
            ProjectFieldType.Number => false,
            ProjectFieldType.Login => false,
            ProjectFieldType.PinCode => false,
            ProjectFieldType.Uri => false,
            _ => throw new ArgumentException(self.ToString(), nameof(self)),
        };
    }

    /// <summary>
    /// Returns true if field values could be mass added and doesn't require special setup
    /// </summary>
    public static bool SupportsMassAdding(this ProjectFieldType self)
    {
        if (!self.HasValuesList())
        {
            return false;
        }
        return self switch
        {
            ProjectFieldType.Dropdown => true,
            ProjectFieldType.MultiSelect => true,
            ProjectFieldType.ScheduleRoomField => true,
            ProjectFieldType.ScheduleTimeSlotField => false,
            _ => throw new ArgumentException(self.ToString(), nameof(self)),
        };
    }
}
