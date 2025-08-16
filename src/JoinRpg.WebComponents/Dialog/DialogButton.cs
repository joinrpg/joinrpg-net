namespace JoinRpg.WebComponents;

public class DialogButton
{
    /// <summary>
    /// Standard button preset.
    /// </summary>
    /// <seealso cref="ButtonPreset"/>
    public ButtonPreset Preset { get; set; }

    /// <summary>
    /// true to render button as disabled.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// When true, this button will be considered as cancel button.
    /// </summary>
    public bool Cancel { get; set; }

    /// <summary>
    /// Button alignment. By default, all buttons are right-aligned.
    /// </summary>
    public DialogButtonAlignment Alignment { get; set; }

    /// <summary>
    /// Assigns a name to the button. The name is important to identify button across others.
    /// When <see cref="Preset"/> is not <see cref="ButtonPreset.None"/>, the value of this
    /// property is automatically set to the string equivalent of selected enum value.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Text to be displayed on a button. Transferred to the <see cref="JoinButton.Label"/> property.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Icon to be displayed on a button. Transferred to the <see cref="JoinButton.Icon"/> property.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Button style. Transferred to the <see cref="JoinButton.Style"/> property.
    /// </summary>
    /// <seealso cref="VariationStyleEnum"/>
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

    public static DialogButton FromJoinButton(JoinButton self)
    {
        if (self.Preset != ButtonPreset.None)
        {
            return new DialogButton(self.Preset, label: self.Label, style: self.Style);
        }
        else
        {
            return new DialogButton(name: "button", label: self.Label!, style: self.Style);
        }

    }
}
