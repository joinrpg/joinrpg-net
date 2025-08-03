namespace JoinRpg.WebComponents;

public static class DialogButtons
{
    public static readonly IReadOnlyCollection<DialogButton> YesNo =
    [
        new(ButtonPreset.Yes, DialogButtonAlignment.Centered),
        new(ButtonPreset.No, DialogButtonAlignment.Centered)
    ];

    public static readonly IReadOnlyCollection<DialogButton> YesNoCancel =
    [
        ..YesNo
            .Append(new DialogButton(ButtonPreset.Cancel, DialogButtonAlignment.Centered))
    ];

    public static readonly IReadOnlyCollection<DialogButton> Understand =
    [
        new(ButtonPreset.Understand, DialogButtonAlignment.Centered)
    ];

    public static readonly IReadOnlyCollection<DialogButton> SaveCancel =
    [
        new(ButtonPreset.Save),
        new(ButtonPreset.Cancel)
    ];
}
