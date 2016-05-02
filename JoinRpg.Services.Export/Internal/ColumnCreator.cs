using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using JoinRpg.Helpers;

namespace JoinRpg.Services.Export.Internal
{
  class ColumnCreator
  {
    public ColumnCreator(IDictionary<Type, Func<object, string>> displayFunctions, ISet<Type> complexTypes, Type targetType, Func<object, object> baseGetter)
    {
      DisplayFunctions = displayFunctions;
      ComplexTypes = complexTypes;
      TargetType = targetType;
      BaseGetter = baseGetter;
    }

    private IDictionary<Type, Func<object, string>> DisplayFunctions { get; }
    private ISet<Type> ComplexTypes { get; }
    private Type TargetType { get; }
    private Func<object, object> BaseGetter { get; }


    public IEnumerable<TableColumn> ParseColumns()
    {
      foreach (var propertyInfo in TargetType.GetProperties())
      {
        var displayColumnAttribute = propertyInfo.DeclaringType.GetCustomAttribute<DisplayColumnAttribute>();

        if (displayColumnAttribute != null)
        {
          yield return
            GetTableColumn(propertyInfo.DeclaringType?.GetProperty(displayColumnAttribute.DisplayColumn) ?? propertyInfo);
        }
        else
        {
          if (ComplexTypes.Contains(propertyInfo.PropertyType))
          {
            var creator = new ColumnCreator(DisplayFunctions, ComplexTypes, propertyInfo.PropertyType, BaseGetter);
            foreach (var childColumn  in creator.ParseColumns())
            {
              yield return childColumn;
            }
          }
          else
          {
            yield return GetTableColumn(propertyInfo);
          }
          
        }
      }
    }

    private TableColumn GetTableColumn([NotNull] PropertyInfo propertyInfo)
    {
      //TODO: BaseGetter
      var tableColumn = new TableColumn()
      {
        Name = propertyInfo.Name,
        Getter = LambdaHelpers.CompileGetter(propertyInfo)
      };

      var displayAttribute = propertyInfo.GetCustomAttribute<DisplayAttribute>();
      if (displayAttribute != null)
      {
        tableColumn.Name = displayAttribute.Name ?? tableColumn.Name;
      }

      if (propertyInfo.GetCustomAttribute<UrlAttribute>() != null)
      {
        tableColumn.IsUri = true;
      }

      tableColumn.Converter = GetConverterForType(propertyInfo.PropertyType);

      return tableColumn;
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
        return LambdaHelpers.GetEnumerableConvertor(item => item.ToString());
      }
      return
        DisplayFunctions.Where(
          displayFunction => displayFunction.Key.IsAssignableFrom(propertyType))
          .Select(kv => kv.Value)
          .FirstOrDefault() ?? (arg => arg?.ToString());
    }
  }
}
