using System.Collections.Immutable;

namespace JoinRpg.WebComponents;

public static class DialogButtons
{
    public static readonly ImmutableArray<DialogButton> YesNo =
    [
        new(ButtonPreset.Yes, DialogButtonAlignment.Centered),
        new(ButtonPreset.No, DialogButtonAlignment.Centered)
    ];

    public static readonly ImmutableArray<DialogButton> YesNoCancel =
    [
        ..YesNo
            .Append(new DialogButton(ButtonPreset.Cancel, DialogButtonAlignment.Centered))
    ];

    public static readonly ImmutableArray<DialogButton> Understand =
    [
        new(ButtonPreset.Understand, DialogButtonAlignment.Centered)
    ];

    public static readonly ImmutableArray<DialogButton> SaveCancel =
    [
        new(ButtonPreset.Save),
        new(ButtonPreset.Cancel)
    ];
}
