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
            preset.Normal.Label.ShouldBeNull();
            preset.Normal.Icon.ShouldBeNull();
        }
        else
        {
            preset.Normal.Label.ShouldNotBeNull();
            //preset.Normal.Icon.ShouldNotBeNull();
        }
    }
}
