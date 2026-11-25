using System.Text.Json;
using JoinRpg.Common.PrimitiveTypes;

namespace JoinRpg.Common.WebComponents.Test;

public class UserLinkViewModelTest
{
    /// <summary>
    /// Модель ездит по JSON между хостом и компонентом — slug обязан переживать сериализацию,
    /// иначе локатор хоста его не увидит (issue #6302).
    /// </summary>
    [Fact]
    public void Slug_SurvivesJsonRoundTrip()
    {
        var model = new UserLinkViewModel(new UserIdentification(42), "Иванов", ViewMode.Show, "ivanov");

        var restored = JsonSerializer.Deserialize<UserLinkViewModel>(JsonSerializer.Serialize(model));

        restored.ShouldBe(model);
        restored!.Slug.ShouldBe("ivanov");
    }

    [Fact]
    public void Slug_Optional_DefaultsToNull()
    {
        var model = new UserLinkViewModel(new UserIdentification(42), "Иванов", ViewMode.Show);

        model.Slug.ShouldBeNull();
    }

    /// <summary>JSON без slug (модель старого хоста) десериализуется со Slug = null.</summary>
    [Fact]
    public void Slug_MissingInJson_DeserializesAsNull()
    {
        var json = JsonSerializer.Serialize(new { UserId = "42", DisplayName = "Иванов", ViewMode = ViewMode.Show });

        var restored = JsonSerializer.Deserialize<UserLinkViewModel>(json);

        restored!.Slug.ShouldBeNull();
    }
}
