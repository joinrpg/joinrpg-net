using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace JoinRpg.Helpers.Web
{
    public class BooleanRequired : RequiredAttribute, IClientModelValidator
    {
        /// <inheritdoc />
        public override bool IsValid(object value)
        {
            return value != null && (bool)value;
        }

        private bool MergeAttribute(IDictionary<string, string> attributes, string key, string value)
        {
            if (attributes.ContainsKey(key))
            {
                return false;
            }

            attributes.Add(key, value);
            return true;
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(context.Attributes, "data-val-brequired", ErrorMessage);
        }
    }
}
