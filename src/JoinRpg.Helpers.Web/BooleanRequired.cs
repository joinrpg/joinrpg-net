using System;
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
            if (value is null)
            {
                return false;
            }

            if (value.GetType() != typeof(bool))
            {
                throw new InvalidOperationException("Can only be used on boolean properties.");
            }

            return (bool) value;
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
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(context.Attributes, "data-val-enforcetrue", ErrorMessage);
        }
    }
}
