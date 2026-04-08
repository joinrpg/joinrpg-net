using System.ComponentModel.DataAnnotations;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.DataModel.Finances;

public class PriceValue
{
    /// <summary>
    /// Database Id of a project.
    /// </summary>
    public int ProjectId { get; set; }

    /// <summary>
    /// Database Id of a date-time column.
    /// </summary>
    public int ProjectFeeSettingId { get; set; }

    /// <summary>
    /// Database Id of a field.
    /// </summary>
    public int FieldId { get; set; }

    /// <summary>
    /// Database Id of a field value (only for <see cref="ProjectFieldType.Dropdown"/> and <see cref="ProjectFieldType.MultiSelect"/>).
    /// </summary>
    public int? FieldValueId { get; set; }

    /// <summary>
    /// Price value (only positive values are allowed).
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Fee should be positive.")]
    public int Price { get; set; }
}
