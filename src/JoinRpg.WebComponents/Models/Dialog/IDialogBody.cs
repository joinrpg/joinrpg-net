using Microsoft.AspNetCore.Components;

namespace JoinRpg.WebComponents;

/// <summary>
/// Implement this interface by component to be placed inside dialog to access capabilities of the <see cref="IDialog"/>.
/// </summary>
public interface IDialogBody
{
    /// <summary>
    /// Reference to the outer dialog component.
    /// Must be decorated with <see cref="CascadingParameterAttribute"/> when implemented in particular dialog body component.
    /// </summary>
    IDialog? Dialog { get; set; }
}
