﻿using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Domain
{
  public static class FieldExtensions
  {
    public static bool HasValueList(this ProjectField field)
    {
      return field.FieldType == ProjectFieldType.Dropdown || field.FieldType == ProjectFieldType.MultiSelect;
    }

    public static bool SupportsMarkdown([NotNull] this ProjectField field)
    {
      return field.FieldType == ProjectFieldType.Text;
    }

    public static bool HasSpecialGroup(this ProjectField field)
    {
      return field.HasValueList() && field.FieldBoundTo == FieldBoundTo.Character;
    }

    public static string GetSpecialGroupName(this ProjectFieldDropdownValue fieldValue)
    {
      return $"{fieldValue.Label}";
    }

    public static string GetSpecialGroupName(this ProjectField field)
    {
      return $"{field.FieldName}";
    }

    public static bool IsAvailableForTarget(this ProjectField field, IClaimSource target)
    {
      return field.IsActive
        && (field.FieldBoundTo == FieldBoundTo.Claim || field.ValidForNpc || !target.IsNpc())
        && (!field.GroupsAvailableFor.Any() || target.IsPartOfAnyOfGroups(field.GroupsAvailableFor));
    }

    [NotNull]
    public static int[] GenerateSpecialGroupsList([NotNull, ItemNotNull] this IEnumerable<FieldWithValue> fieldValues)
    {
      if (fieldValues == null) throw new ArgumentNullException(nameof(fieldValues));
      return fieldValues.SelectMany(v => v.GetSpecialGroupsToApply().Select(g =>g.CharacterGroupId)).ToArray();
    }

    public static bool CanHaveValue(this ProjectField projectField)
    {
      return projectField.FieldType != ProjectFieldType.Header;
    }
  }
}
