using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using JoinRpg.Helpers;

namespace JoinRpg.Services.Export.AutoFrontEnd
{
    internal class ColumnCreator
    {
        public ColumnCreator(IDictionary<Type, Func<object, string>> displayFunctions, Type targetType)
        {
            DisplayFunctions = displayFunctions;
            TargetType = targetType;
        }

        private IDictionary<Type, Func<object, string>> DisplayFunctions { get; }
        private Type TargetType { get; }


        public IEnumerable<TableColumn> ParseColumns()
        {
            foreach (var propertyInfo in TargetType.GetProperties())
            {
                var displayColumnAttribute = propertyInfo.DeclaringType?.GetCustomAttribute<DisplayColumnAttribute>();

                if (displayColumnAttribute != null)
                {
                    yield return
                      GetTableColumn(propertyInfo.DeclaringType?.GetProperty(displayColumnAttribute.DisplayColumn) ?? propertyInfo);
                }
                else
                {
                    yield return GetTableColumn(propertyInfo);
                }
            }
        }

        private TableColumn GetTableColumn([NotNull] PropertyInfo propertyInfo)
        {
            return new TableColumn
            {
                Getter = LambdaHelpers.CompileGetter(propertyInfo, TargetType),
                Name = propertyInfo.GetDisplayName(),
                Converter = GetConverterForType(propertyInfo.PropertyType),
            };
        }

        private Func<object, string> GetConverterForType(Type propertyType)
        {
            if (propertyType.IsEnum)
            {
                return o => ((Enum)o).GetDisplayName();
            }
            if (typeof(string).IsAssignableFrom(propertyType))
            {
                return o => (string)o; //Prevent futher conversion
            }

            var enumerableArg = LambdaHelpers.GetEnumerableType(propertyType);
            if (enumerableArg != null)
            {
                return LambdaHelpers.GetEnumerableConvertor(GetConverterForType(enumerableArg));
            }

            if (typeof(IEnumerable).IsAssignableFrom(propertyType))
            {
                return LambdaHelpers.GetEnumerableConvertor(item => item.ToString()!);
            }
            return
              DisplayFunctions.Where(displayFunction => displayFunction.Key.IsAssignableFrom(propertyType))
                .Select(kv => kv.Value)
                .FirstOrDefault() ?? (arg => arg?.ToString());
        }
    }
}
