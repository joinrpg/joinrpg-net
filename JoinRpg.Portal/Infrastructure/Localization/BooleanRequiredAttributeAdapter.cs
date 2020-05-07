using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Helpers.Web;
using JoinRpg.Portal.Resources;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;

namespace JoinRpg.Portal.Infrastructure.Localization
{
    public class BooleanRequiredAttributeAdapter : AttributeAdapterBase<BooleanRequired>
    {
        public BooleanRequiredAttributeAdapter(BooleanRequired attribute, IStringLocalizer stringLocalizer)
            : base(attribute, stringLocalizer)
        { }

        public override void AddValidation(ClientModelValidationContext context) { }

        public override string GetErrorMessage(ModelValidationContextBase validationContext)
        {
            return GetErrorMessage(validationContext.ModelMetadata, validationContext.ModelMetadata.GetDisplayName());
        }
    }

    public class BooleanRequiredAttributeAdapterProvider : IValidationAttributeAdapterProvider
    {
        private readonly IValidationAttributeAdapterProvider fallback = new ValidationAttributeAdapterProvider();

        public IAttributeAdapter GetAttributeAdapter(ValidationAttribute attribute, IStringLocalizer stringLocalizer)
        {
            var attr = attribute as BooleanRequired;
            return attr == null ?
                fallback.GetAttributeAdapter(attribute, stringLocalizer) :
                new BooleanRequiredAttributeAdapter(attr, stringLocalizer);
        }
    }
}
