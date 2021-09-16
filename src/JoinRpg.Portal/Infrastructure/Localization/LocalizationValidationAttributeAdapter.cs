using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;


namespace JoinRpg.Portal.Infrastructure.Localization
{
    public class LocalizationValidationAttributeAdapter : AttributeAdapterBase<ValidationAttribute>
    {
        private readonly IStringLocalizer _stringLocalizer;
        public LocalizationValidationAttributeAdapter(ValidationAttribute attribute, IStringLocalizer stringLocalizer)
            : base(attribute, stringLocalizer)
        {
            _stringLocalizer = stringLocalizer;
        }

        public override void AddValidation(ClientModelValidationContext context) { }

        ///<summary>
        ///
        /// Called for any attribute inheriting <seealso cref="ValidationAttribute"/>.
        /// Alters the localization way of the error message.
        /// The default localization is
        ///
        ///<code>
        ///     localizer[Attribute.ErrorMessage]
        ///</code>
        /// 
        ///  where value of <c>ErrorMessage</c> itself serves as localizer key.
        ///  The altered localization way is
        ///
        /// <code>
        ///     localizer[key]
        /// </code>
        /// 
        ///  where <c>"key"</c> is formed based on the class name, field name and attribute name values
        ///  and has the format:
        ///  
        ///       <code>Namespace.ClassName.FieldName.AttributeTypeName.AttributeProperty</code>
        /// 
        ///  where <br /><c>Namespace.ClassName</c> - full name of the type, which validated property belongs to.
        ///  <br />
        ///  <c>FieldName</c> - a name of a field, which has the given attribute, and which is currently validated.
        ///  <br />
        ///  <c>AttributeName</c> - the name of the type of the given attribute, excluding "Attribute" suffix.
        ///  <br /><br />
        ///  Example:
        ///     "Display" instead of "DisplayAttribute"
        /// <br /><br />
        /// <c>AttributeProperty</c> - the property name of an attribute, which is being localized.
        /// <br /><br />
        /// Examples: Display.Name, Required.ErrorMessage
        /// </summary>
        public override string GetErrorMessage(ModelValidationContextBase validationContext)
        {
            if (validationContext.ModelMetadata == null)
            {
                throw new ArgumentNullException(nameof(validationContext.ModelMetadata));
            }

            var objectType = validationContext.ModelMetadata.ContainerType;
            var memberName = validationContext.ModelMetadata.Name;
            var defaultErrorMessage = Attribute.FormatErrorMessage(validationContext.ModelMetadata.GetDisplayName());

            if (string.IsNullOrEmpty(Attribute.ErrorMessageResourceName) && Attribute.ErrorMessageResourceType == null)
            {
                var key = LocalizationService.GenerateLocalizationKey(objectType, Attribute.GetType(), memberName, nameof(Attribute.ErrorMessage));
                //the property formatting of an error message should be ensured. Since every
                //attribute has its own formatting logic, it's better to temporarily swap
                //error messages.
                var defaultAttributeErrorMessage = Attribute.ErrorMessage;
                //localized message
                Attribute.ErrorMessage = LocalizationService.GetLocalizedString(_stringLocalizer, key, defaultErrorMessage);
                //localized formatted error message
                var formattedErrorMessage = Attribute.FormatErrorMessage(validationContext.ModelMetadata.GetDisplayName());
                //change back to the default value of the attribute error message
                Attribute.ErrorMessage = defaultAttributeErrorMessage;
                return formattedErrorMessage;
            }

            return defaultErrorMessage;
        }
    }

    ///<summary>
    ///
    /// Provides the default validation attribute adapter for all validation attributes of any type,
    /// which inherits ValidationAttribute.
    /// Attribute adapter is required to override the logic of error message formation.
    /// This provider is registered in DI container as a service.
    ///
    /// </summary>
    ///
    public class LocalizationValidationAttributeAdapterProvider : IValidationAttributeAdapterProvider
    {
        public IAttributeAdapter GetAttributeAdapter(ValidationAttribute attribute, IStringLocalizer stringLocalizer)
        {
            return new LocalizationValidationAttributeAdapter(attribute, stringLocalizer);
        }
    }
}
