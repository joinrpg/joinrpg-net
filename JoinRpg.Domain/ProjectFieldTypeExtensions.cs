﻿using JoinRpg.DataModel;

namespace JoinRpg.Domain
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
                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns true if price could be entered for field, not for it's values
        /// </summary>
        public static bool PriceEditable(this ProjectFieldType self)
        {
            switch (self)
            {
                case ProjectFieldType.Checkbox:
                case ProjectFieldType.Number:
                    return true;
                default:
                    return false;
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
                default:
                    return false;
            }
        }
    }
}
