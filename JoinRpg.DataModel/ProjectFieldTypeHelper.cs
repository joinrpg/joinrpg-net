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
                    return false;
                default:
                    throw new ArgumentException(nameof(self));
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
                    return false;
                default:
                    throw new ArgumentException(nameof(self));
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
                    return true;
                case ProjectFieldType.String:
                case ProjectFieldType.Text:
                case ProjectFieldType.Checkbox:
                case ProjectFieldType.Header:
                case ProjectFieldType.Number:
                case ProjectFieldType.Login:
                    return false;
                default:
                    throw new ArgumentException(nameof(self));
            }
        }
    }
}
