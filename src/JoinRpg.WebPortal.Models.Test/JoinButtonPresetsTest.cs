using JoinRpg.WebComponents;

namespace JoinRpg.WebPortal.Models.Test;

public class JoinButtonPresetsTest
{
    [Theory]
    [ClassData(typeof(EnumTheoryDataGenerator<ButtonPreset>))]
    public void PresetShouldBeConfigured(ButtonPreset preset)
    {
        JoinButton.Presets.ContainsKey(preset).ShouldBeTrue("Each preset must be configured");
    }

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
            var label = preset.Normal.Label?.Trim();
            var icon = preset.Normal.Icon?.Trim();
            (!string.IsNullOrEmpty(label) || !string.IsNullOrEmpty(icon)).ShouldBeTrue("Label or icon or both must be specified");
        }
    }
}
