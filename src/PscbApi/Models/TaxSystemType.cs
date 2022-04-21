// ReSharper disable IdentifierTypo
using Newtonsoft.Json;

namespace PscbApi.Models;

/// <summary>
/// Tax system types
/// </summary>
[JsonConverter(typeof(IdentifiableEnumConverter))]
public enum TaxSystemType
{
    /// <summary>
    /// General taxation system
    /// </summary>
    [Identifier("osn")]
    General,

    /// <summary>
    /// Simplified taxation system (income)
    /// </summary>
    [Identifier("usn_income")]
    SimplifiedIncome,

    /// <summary>
    /// Simplified taxation system (income - outcome)
    /// </summary>
    [Identifier("usn_income_outcome")]
    SimplifiedIncomeOutcome,

    /// <summary>
    /// Unified tax on imputed income
    /// </summary>
    [Identifier("envd")]
    UnifiedTaxOnImputedIncome,

    /// <summary>
    /// Unified agricultural tax
    /// </summary>
    [Identifier("esn")]
    UnifiedAgriculturalTax,

    /// <summary>
    /// Patented tax system
    /// </summary>
    [Identifier("patent")]
    Patent,
}
