using JoinRpg.WebComponents;

namespace JoinRpg.WebPortal.Models.Test;

public class JoinButtonPresetsTest
{
    [Theory]
    [ClassData(typeof(EnumTheoryDataGenerator<ButtonPreset>))]
    public void PresetsShouldBeRecognized(ButtonPreset buttonPreset)
    {
        var preset = JoinButton.Presets[buttonPreset];
        if (buttonPreset == ButtonPreset.None)
        {
            preset.ShouldBeNull();
        }
        else
        {
            preset.ShouldNotBeNull();
        }
    }
}
