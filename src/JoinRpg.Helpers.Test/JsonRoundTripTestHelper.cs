using System.Text.Json;

namespace JoinRpg.Helpers.Test;

public static class JsonRoundTripTestHelper
{
    extension<TItem>(TItem instance) where TItem : class
    {
        public void ShouldRoundTripThroughJson()
        {
            var serialized = JsonSerializer.Serialize(instance).ShouldNotBeNull();
            var deserialized = JsonSerializer.Deserialize<TItem>(serialized).ShouldNotBeNull();
            deserialized.ShouldBeEquivalentTo(instance);
        }
    }
}
