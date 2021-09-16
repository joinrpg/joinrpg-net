using System;

namespace JoinRpg.DataModel
{
    public static class ProjectFieldTypeHelper
    {
        /// <summary>
        /// Returns true if field supports price calculations
        /// </summary>
        public static bool SupportsPricing(this ProjectFieldType self)
        {
            switch (self)
            {
                case ProjectFieldType.Dropdown:
                case ProjectFieldType.MultiSelect:
                case ProjectFieldType.Checkbox:
                case ProjectFieldType.Number:
                    return true;
                case ProjectFieldType.String:
                case ProjectFieldType.Text:
                case ProjectFieldType.Header:
                case ProjectFieldType.Login:
                case ProjectFieldType.ScheduleRoomField:
                case ProjectFieldType.ScheduleTimeSlotField:
                case ProjectFieldType.PinCode:
                    return false;
                default:
                    throw new ArgumentException(self.ToString(), nameof(self));
            }
        }

        /// <summary>
        /// Returns true if price could be entered for field, not for it's values
        /// </summary>
        public static bool SupportsPricingOnField(this ProjectFieldType self)
        {
            switch (self)
            {
                case ProjectFieldType.Checkbox:
                case ProjectFieldType.Number:
                    return true;
                case ProjectFieldType.String:
                case ProjectFieldType.Text:
                case ProjectFieldType.Dropdown:
                case ProjectFieldType.MultiSelect:
                case ProjectFieldType.Header:
                case ProjectFieldType.Login:
                case ProjectFieldType.ScheduleRoomField:
                case ProjectFieldType.ScheduleTimeSlotField:
                case ProjectFieldType.PinCode:
                    return false;
                default:
                    throw new ArgumentException(self.ToString(), nameof(self));
            }
        }

        /// <summary>
        /// Returns true if field has predefined values
        /// </summary>
        public static bool HasValuesList(this ProjectFieldType self)
        {
            switch (self)
            {
                case ProjectFieldType.Dropdown:
                case ProjectFieldType.MultiSelect:
                case ProjectFieldType.ScheduleRoomField:
                case ProjectFieldType.ScheduleTimeSlotField:
                    return true;
                case ProjectFieldType.String:
                case ProjectFieldType.Text:
                case ProjectFieldType.Checkbox:
                case ProjectFieldType.Header:
                case ProjectFieldType.Number:
                case ProjectFieldType.Login:
                case ProjectFieldType.PinCode:
                    return false;
                default:
                    throw new ArgumentException(self.ToString(), nameof(self));
            }
        }

        /// <summary>
        /// Returns true if field values could be mass added and doesn't require special setup
        /// </summary>
        public static bool SupportsMassAdding(this ProjectFieldType self)
        {
            return self switch
            {
                ProjectFieldType.Dropdown => true,
                ProjectFieldType.MultiSelect => true,
                ProjectFieldType.ScheduleRoomField => true,
                ProjectFieldType.ScheduleTimeSlotField => false,
                ProjectFieldType.String => false,
                ProjectFieldType.Text => false,
                ProjectFieldType.Checkbox => false,
                ProjectFieldType.Header => false,
                ProjectFieldType.Number => false,
                ProjectFieldType.Login => false,
                ProjectFieldType.PinCode => false,
                _ => throw new ArgumentException(self.ToString(), nameof(self)),
            };
        }
    }
}
