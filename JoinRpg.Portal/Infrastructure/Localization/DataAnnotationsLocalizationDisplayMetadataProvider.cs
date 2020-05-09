using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace JoinRpg.Portal.Infrastructure.Localization
{
    public class DataAnnotationsLocalizationDisplayMetadataProvider : IDisplayMetadataProvider
    {
        private readonly IStringLocalizerFactory _stringLocalizerFactory;
        private readonly MvcDataAnnotationsLocalizationOptions _localizationOptions;

        public DataAnnotationsLocalizationDisplayMetadataProvider(
            IOptions<MvcDataAnnotationsLocalizationOptions> localizationOptions,
            IStringLocalizerFactory stringLocalizerFactory)
        {
            if (localizationOptions == null)
            {
                throw new ArgumentNullException(nameof(localizationOptions));
            }

            if (stringLocalizerFactory == null)
            {
                throw new ArgumentNullException(nameof(stringLocalizerFactory));
            }

            _localizationOptions = localizationOptions.Value;
            _stringLocalizerFactory = stringLocalizerFactory;
        }

        ///<summary>
        ///
        /// Alters the localization way of <seealso cref="DisplayAttribute"/> and <seealso cref="DisplayNameAttribute"/> default core attributes.
        /// <br/><br/>
        /// Changes the metadata for any attribute, which is of Display or DisplayName
        /// or inherits two forementioned types.
        /// "Changes the metadata" means that there is special "metadata" object in the context,
        /// which store various information about Display attributes, including
        /// the localization functions, which define the way of localization of Name, Description and etc
        /// properties of Display attributes.
        ///<br/><br/>
        /// The default localization is
        ///
        ///      <code>localizer[Attribute.Name] ("Name" is an example property here)</code> 
        ///
        /// where value of <c>"Name"</c> itself serves as localizer key.
        /// The altered localization way is
        ///
        ///      <code>localizer[key]</code>
        ///
        /// where <c>"key"</c> is formed based on the class name, field name and attribute name values
        /// and has the format:
        /// 
        ///      <code>Namespace.ClassName.FieldName.AttributeTypeName.AttributeProperty</code>
        ///
        /// where
        /// <br/><c>Namespace.ClassName</c> - full name of the type, which validated property belongs to.
        ///<br/>
        /// <c>FieldName</c> - a name of a field, which has the given attribute, and which is currently validated.
        ///<br/>
        /// <c>AttributeName</c> - the name of the type of the given attribute, excluding "Attribute" suffix.
        ///<br/><br/>
        /// Example: "Display" instead of "DisplayAttribute"
        ///<br/><br/>
        /// <c>AttributeProperty</c> - the property name of an attribute, which is being localized.
        /// <br/><br/>Examples: Display.Name, Required.ErrorMessage
        /// </summary>
        public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
        {
            if (_localizationOptions.DataAnnotationLocalizerProvider == null)
            {
                return;
            }

            var containerType = context.Key.ContainerType ?? context.Key.ModelType;
            IStringLocalizer localizer = _localizationOptions.DataAnnotationLocalizerProvider(containerType, _stringLocalizerFactory);

            if (localizer == null)
            {
                return;
            }

            var displayMetadata = context.DisplayMetadata;

            var displayNameAttribute = context.Attributes.OfType<DisplayNameAttribute>().FirstOrDefault();
            var displayAttribute = context.Attributes.OfType<DisplayAttribute>().FirstOrDefault();

            //Description
            if (displayAttribute != null && displayAttribute.ResourceType == null)
            {
                string key = GenerateLocalizationKeyFromContext<DisplayAttribute>("Description", context);
                displayMetadata.Description = () => LocalizationService.GetLocalizedString(localizer, key, displayAttribute.Description);
            }

            // Name
            if (displayAttribute?.GetName() != null && displayAttribute.ResourceType == null)
            {
                string key = GenerateLocalizationKeyFromContext<DisplayAttribute>("Name", context);
                displayMetadata.DisplayName = () => LocalizationService.GetLocalizedString(localizer, key, displayAttribute.Name);
            }

            if (displayAttribute?.GetName() == null && displayNameAttribute != null && localizer != null)
            {
                string key = GenerateLocalizationKeyFromContext<DisplayNameAttribute>("DisplayName", context);
                displayMetadata.DisplayName = () => LocalizationService.GetLocalizedString(localizer, key, displayNameAttribute.DisplayName);
            }

            //Enums
            var underlyingType = Nullable.GetUnderlyingType(context.Key.ModelType) ?? context.Key.ModelType;
            var underlyingTypeInfo = underlyingType.GetTypeInfo();
            var groupedDisplayNamesAndValues = new List<KeyValuePair<EnumGroupAndName, string>>();
            var namesAndValues = new Dictionary<string, string>();

            if (underlyingTypeInfo.IsEnum)
            {
                var enumFields = Enum
                    .GetNames(underlyingType)
                    .Select(name => underlyingType.GetField(name))
                    .OrderBy(field => field.GetCustomAttribute<DisplayAttribute>(inherit: false)?.GetOrder() ?? 1000);

                foreach (var field in enumFields)
                {
                    var groupName = GetDisplayGroup(field);
                    var value = ((Enum)field.GetValue(obj: null)).ToString("d");

                    groupedDisplayNamesAndValues.Add
                       (
                            new KeyValuePair<EnumGroupAndName, string>
                            (
                                new EnumGroupAndName
                                (
                                    groupName,
                                    () => GetDisplayName(field, localizer)
                                ),
                                value
                            )
                       );
                    namesAndValues.Add(field.Name, value);
                }

                displayMetadata.EnumGroupedDisplayNamesAndValues = groupedDisplayNamesAndValues;
                displayMetadata.EnumNamesAndValues = namesAndValues;
            }

            //Prompt/Placeholder
            if (displayAttribute != null && displayAttribute.ResourceType == null)
            {
                string key = GenerateLocalizationKeyFromContext<DisplayAttribute>("Prompt", context);
                displayMetadata.Placeholder = () => LocalizationService.GetLocalizedString(localizer, key, displayAttribute.Prompt);
            }

        }

        private string GenerateLocalizationKeyFromContext<AttributeType>(string attributePropertyName, DisplayMetadataProviderContext context)
        {
            Type classType = context.Key.PropertyInfo?.DeclaringType ?? context.Key.ModelType;
            Type attributeType = typeof(AttributeType);
            string memberName = context.Key.Name;

            return LocalizationService.GenerateLocalizationKey(classType, attributeType, memberName, attributePropertyName);
        }

        private static string GetDisplayGroup(FieldInfo field)
        {
            return field.GetCustomAttribute<DisplayAttribute>(inherit: false)?.GetGroupName() ?? string.Empty;
        }

        private string GetDisplayName(FieldInfo field, IStringLocalizer stringLocalizer)
        {
            var display = field.GetCustomAttribute<DisplayAttribute>(inherit: false);

            if (display == null)
            {
                return field.Name;
            }
            var name = display.GetName();
            var key = LocalizationService.GenerateLocalizationKey(field.DeclaringType, typeof(DisplayAttribute), field.Name, "Name");

            if (stringLocalizer != null && display.ResourceType == null)
            {
                name = LocalizationService.GetLocalizedString(stringLocalizer, key, display.Name ?? field.Name);
            }

            return name ?? field.Name;
        }
    }
}
