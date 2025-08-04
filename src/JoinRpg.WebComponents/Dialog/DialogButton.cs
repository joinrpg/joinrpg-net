namespace JoinRpg.WebComponents;

public class DialogButton
{
    /// <inheritdoc />
    public ButtonPreset Preset { get; set; }

    /// <inheritdoc />
    public bool Disabled { get; set; }

    /// <inheritdoc />
    public bool Cancel { get; set; }

    /// <inheritdoc />
    public DialogButtonAlignment Alignment { get; set; }

    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public string? Label { get; set; }

    /// <inheritdoc />
    public string? Icon { get; set; }

    /// <inheritdoc />
    public VariationStyleEnum? Style { get; set; }

    /// <summary>
    /// Initializes a new instance of custom dialog button.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="label"></param>
    /// <param name="icon"></param>
    /// <param name="style"></param>
    /// <param name="alignment"></param>
    /// <param name="disabled"></param>
    /// <param name="cancel"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public DialogButton(
        string name,
        string label,
        string? icon = null,
        VariationStyleEnum? style = null,
        DialogButtonAlignment alignment = DialogButtonAlignment.Right,
        bool disabled = false,
        bool cancel = false)
    {
        Name = (name ?? throw new ArgumentNullException(nameof(name), "Button name must not be null")).Trim();
        if (string.IsNullOrEmpty(Name))
        {
            throw new ArgumentException("Button name must not be empty string", nameof(name));
        }

        Label = (label ?? throw new ArgumentNullException(nameof(label), "Button label must not be null")).Trim();
        if (string.IsNullOrEmpty(Label))
        {
            throw new ArgumentException("Button label must not be empty string", nameof(label));
        }

        Icon = icon?.Trim();
        Style = style;
        Alignment = alignment;
        Disabled = disabled;
        Cancel = cancel;
    }

    /// <summary>
    /// Initializes a new instance of predefined dialog button.
    /// </summary>
    /// <param name="preset"></param>
    /// <param name="alignment"></param>
    /// <param name="disabled"></param>
    /// <param name="label"></param>
    /// <param name="icon"></param>
    /// <param name="style"></param>
    /// <param name="cancel"></param>
    /// <exception cref="ArgumentException"></exception>
    public DialogButton(
        ButtonPreset preset,
        DialogButtonAlignment alignment = DialogButtonAlignment.Right,
        bool disabled = false,
        string? label = null,
        string? icon = null,
        VariationStyleEnum? style = null,
        bool cancel = false)
    {
        if (preset == ButtonPreset.None)
        {
            throw new ArgumentException("Use another constructor to create a custom dialog button", nameof(preset));
        }

        Preset = preset;
        Name = preset.ToString();
        Alignment = alignment;
        Disabled = disabled;
        Label = label?.Trim();
        Icon = icon?.Trim();
        Style = style;
        Cancel = cancel;
    }
}
